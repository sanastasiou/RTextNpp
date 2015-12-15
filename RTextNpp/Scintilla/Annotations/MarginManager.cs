using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;

namespace RTextNppPlugin.Scintilla.Annotations
{
    class MarginManager : ErrorBase, IError
    {
        #region [Data Members]
        private const Settings.RTextNppSettings SETTING             = Settings.RTextNppSettings.EnableErrorMarkers;
        private double _currentPixelFactorMainScintilla             = 0.0;
        private double _currentPixelFactorSecondScintilla           = 0.0;
        private const double PIXEL_ZOOM_FACTOR                      = 24.0;
        private const int ERROR_DESCRIPTION_MARGIN                  = Constants.Scintilla.SC_MAX_MARGIN - 1;
        private const double MAX_ZOOM_LEVEL                         = 30.0;
        private const double ZOOM_LEVEL_OFFSET                      = 10.0;
        private int _maxCharLengthMainSci                           = 0;
        private int _maxCharLengthSubSci                            = 0;
        private VoidDelayedEventHandler _paintedEventHandler        = null;
        private int _lineNumberStyleBackground                      = int.MinValue;
        private IStyleConfigurationObserver _styleObserver          = null;
        #endregion

        #region [Interface]
        internal MarginManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot, IStyleConfigurationObserver styleObserver, double updateDelay = Constants.Scintilla.ANNOTATIONS_UPDATE_DELAY) :
            base(settings, nppHelper, plugin, workspaceRoot, updateDelay)
        {
            _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
            plugin.ScintillaZoomChanged += OnScintillaZoomChanged;
            //set margin 4 to be text margin
            _nppHelper.SetMarginTypeN(_nppHelper.MainScintilla, ERROR_DESCRIPTION_MARGIN, SciMsg.SC_MARGIN_TEXT);
            _nppHelper.SetMarginTypeN(_nppHelper.SecondaryScintilla, ERROR_DESCRIPTION_MARGIN, SciMsg.SC_MARGIN_TEXT);

            //force margin to use line number background
            _nppHelper.SetMarginMaskN(_nppHelper.MainScintilla, ERROR_DESCRIPTION_MARGIN, 0);
            _nppHelper.SetMarginMaskN(_nppHelper.SecondaryScintilla, ERROR_DESCRIPTION_MARGIN, 0);

            //initialize pixel factors
            _currentPixelFactorMainScintilla   = CalculatedPixels(_nppHelper.GetZoomLevel(_nppHelper.MainScintilla));
            _currentPixelFactorSecondScintilla = CalculatedPixels(_nppHelper.GetZoomLevel(_nppHelper.SecondaryScintilla));

            //initialize painted event debouncer
            _paintedEventHandler                = new VoidDelayedEventHandler(UpdateBackgroundMargin, updateDelay);
            //plugin.ScintillaUiPainted           += OnScintillaUiPainted;
            _styleObserver = styleObserver;
            NormalizeMarginsBackground();
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
            }
            ProcessSettingChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if(disposing)
            {
                Plugin.Instance.ScintillaUiPainted -= OnScintillaUiPainted;
            }
            base.Dispose(disposing);

        }

        protected override Constants.StyleId ConvertSeverityToStyleId(ErrorItemViewModel.SeverityType severity)
        {
            switch (severity)
            {
                case ErrorItemViewModel.SeverityType.Debug:
                    return Constants.StyleId.MARGIN_DEBUG;
                case ErrorItemViewModel.SeverityType.Info:
                    return Constants.StyleId.MARGIN_INFO;
                case ErrorItemViewModel.SeverityType.Warning:
                    return Constants.StyleId.MARGIN_WARNING;
                case ErrorItemViewModel.SeverityType.Error:
                    return Constants.StyleId.MARGIN_ERROR;
                case ErrorItemViewModel.SeverityType.Fatal:
                    return Constants.StyleId.MARGIN_FATAL_ERROR;
                default:
                    return Constants.StyleId.MARGIN_ERROR;
            }
        }

        #endregion

        #region [Event Handlers]

        private void OnScintillaUiPainted()
        {
            int previousLineNumberStyleBackgroundMain      = _lineNumberStyleBackground;
            int currentLineNumberStyleBackgroundMain       = _nppHelper.GetStyleBackground(_nppHelper.MainScintilla, (int)SciMsg.STYLE_LINENUMBER);

            if (previousLineNumberStyleBackgroundMain != currentLineNumberStyleBackgroundMain)
            {
                _paintedEventHandler.TriggerHandler();
            }
        }

        void OnScintillaZoomChanged(IntPtr sciPtr, int newZoomLevel)
        {
            if (newZoomLevel > (-ZOOM_LEVEL_OFFSET))
            {
                int previousMaxLength = GetMaxCharLength(sciPtr);
                if (sciPtr == _nppHelper.MainScintilla)
                {
                    _currentPixelFactorMainScintilla = CalculatedPixels(newZoomLevel);
                    if (_lastMainViewAnnotatedFile != string.Empty)
                    {
                        _nppHelper.SetMarginWidthN(sciPtr, ERROR_DESCRIPTION_MARGIN, (int)Math.Ceiling(((double)previousMaxLength * _currentPixelFactorMainScintilla)));
                    }
                }
                else
                {
                    _currentPixelFactorSecondScintilla = CalculatedPixels(newZoomLevel);
                    if (_lastSubViewAnnotatedFile != string.Empty)
                    {
                        _nppHelper.SetMarginWidthN(sciPtr, ERROR_DESCRIPTION_MARGIN, (int)Math.Ceiling(((double)previousMaxLength * _currentPixelFactorSecondScintilla)));
                    }
                }
            }
        }        

        #endregion

        #region [Helpers]

        protected override object OnBufferActivated(string file)
        {
            PreProcessOnBufferActivatedEvent();
            if (!Utilities.FileUtilities.IsRTextFile(file, _settings, _nppHelper))
            {
                //remove annotations from the view which this file belongs to
                var scintilla = _nppHelper.FindScintillaFromFilepath(file);
                _nppHelper.ClearAllTextMargins(scintilla);
                _nppHelper.SetMarginWidthN(scintilla, ERROR_DESCRIPTION_MARGIN, 0);
                if (scintilla == _nppHelper.MainScintilla)
                {
                    _lastMainViewAnnotatedFile = string.Empty;
                }
                else
                {
                    _lastSubViewAnnotatedFile = string.Empty;
                }
            }
            return null;
        }

        private void ShowAnnotations(IntPtr scintilla, int maxRequiredLength)
        {
            if (_areAnnotationEnabled)
            {
                _nppHelper.SetMarginWidthN(scintilla, ERROR_DESCRIPTION_MARGIN, (int)Math.Ceiling(((double)maxRequiredLength * GetMarginLength(scintilla))));
            }
        }

        protected override void HideAnnotations(IntPtr scintilla)
        {
            var openFiles = _nppHelper.GetOpenFiles(scintilla);
            var docIndex  = _nppHelper.CurrentDocIndex(scintilla);
            if (docIndex != Constants.Scintilla.VIEW_NOT_ACTIVE && Utilities.FileUtilities.IsRTextFile(openFiles[docIndex], _settings, _nppHelper))
            {
                if ((Utilities.FileUtilities.FindWorkspaceRoot(openFiles[docIndex]) + Path.GetExtension(openFiles[docIndex])).Equals(_workspaceRoot))
                {
                    _nppHelper.ClearAllTextMargins(scintilla);
                    _nppHelper.SetMarginWidthN(scintilla, ERROR_DESCRIPTION_MARGIN, 0);
                }
            }
        }

        protected override void DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr)
        {
            HideAnnotations(sciPtr);
            try
            {
                if (errors != null && GetMarginLength(sciPtr) != 0)
                {
                    int maxCharLenghtForMargin = 0;
                    var aErrorGroupByLines     = errors.ErrorList.GroupBy(y => y.Line).AsParallel();
                    foreach (var errorGroup in aErrorGroupByLines)
                    {
                        _nppHelper.SetMarginStyle(sciPtr, errorGroup.First().LineForScintilla, (int)ConvertSeverityToStyleId(errorGroup.First().Severity));
                        _nppHelper.SetMarginText(sciPtr, errorGroup.First().LineForScintilla, errorGroup.First().Severity.ToString());
                        int aDescriptionLength = errorGroup.First().Severity.ToString().Length;
                        if(aDescriptionLength > maxCharLenghtForMargin)
                        {
                            maxCharLenghtForMargin = aDescriptionLength;
                        }
                    }
                    SetMaxCharLength(sciPtr, maxCharLenghtForMargin);
                    ShowAnnotations(sciPtr, maxCharLenghtForMargin);
                }
            }
            catch (Exception)
            {
                Trace.WriteLine("Draw margins failed.");
            }
        }

        private double CalculatedPixels(int zoomLevel)
        {
            double zoomLevelWithOffset = zoomLevel + ZOOM_LEVEL_OFFSET;
            return (zoomLevelWithOffset / MAX_ZOOM_LEVEL) * PIXEL_ZOOM_FACTOR;
        }

        private double GetMarginLength(IntPtr scintillaPtr)
        {
            if(scintillaPtr == _nppHelper.MainScintilla)
            {
                return _currentPixelFactorMainScintilla;
            }
            return _currentPixelFactorSecondScintilla;
        }

        private int GetMaxCharLength(IntPtr scintillaPtr)
        {
            if(scintillaPtr == _nppHelper.MainScintilla)
            {
                return _maxCharLengthMainSci;
            }
            return _maxCharLengthSubSci;
        }

        private void SetMaxCharLength(IntPtr scintillaPtr, int maxCharLength)
        {
            if (scintillaPtr == _nppHelper.MainScintilla)
            {
                _maxCharLengthMainSci = maxCharLength;
            }
            else
            {
                _maxCharLengthSubSci = maxCharLength;
            }
        }

        private void UpdateBackgroundMargin()
        {
            //update 
        }

        private void NormalizeMarginsBackground()
        {
            _lineNumberStyleBackground = _nppHelper.GetStyleBackground(_nppHelper.MainScintilla, (int)SciMsg.STYLE_LINENUMBER);
            
            //initialize style backgrounds for all margin styles
            for (var i = Constants.StyleId.MARGIN_DEBUG; i < Constants.StyleId.MARGIN_FATAL_ERROR; ++i)
            {
                _nppHelper.SetStyleBackground(_nppHelper.MainScintilla, (int)i, _lineNumberStyleBackground);
                _nppHelper.SetStyleBackground(_nppHelper.SecondaryScintilla, (int)i, _lineNumberStyleBackground);
                //_styleObserver.SaveStyleBackground(i, _lineNumberStyleBackground);
            }
        }
        #endregion
    }
}