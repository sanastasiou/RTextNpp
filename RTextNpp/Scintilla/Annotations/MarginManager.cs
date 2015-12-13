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
        private const double PIXEL_ZOOM_FACTOR                      = 30.0;
        private const int ERROR_DESCRIPTION_MARGIN                  = Constants.Scintilla.SC_MAX_MARGIN - 1;
        private const double MAX_ZOOM_LEVEL                         = 30.0;
        private const double ZOOM_LEVEL_OFFSET                      = 10.0;
        #endregion

        #region [Interface]
        internal MarginManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot, double updateDelay = Constants.Scintilla.ANNOTATIONS_UPDATE_DELAY) :
            base(settings, nppHelper, plugin, workspaceRoot, updateDelay)
        {
            _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
            plugin.ScintillaZoomChanged += OnScintillaZoomChanged;
            //set margin 4 to be text margin
            _nppHelper.SetMarginTypeN(_nppHelper.MainScintilla, ERROR_DESCRIPTION_MARGIN, SciMsg.SC_MARGIN_TEXT);
            _nppHelper.SetMarginTypeN(_nppHelper.SecondaryScintilla, ERROR_DESCRIPTION_MARGIN, SciMsg.SC_MARGIN_TEXT);

            //initialize pixel factors
            _currentPixelFactorMainScintilla   = CalculatedPixels(_nppHelper.GetZoomLevel(_nppHelper.MainScintilla));
            _currentPixelFactorSecondScintilla = CalculatedPixels(_nppHelper.GetZoomLevel(_nppHelper.SecondaryScintilla));
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
            }
            ProcessSettingChanged();
        }

        #endregion

        #region [Event Handlers]

        void OnScintillaZoomChanged(IntPtr sciPtr, int newZoomLevel)
        {
            if (newZoomLevel > (-ZOOM_LEVEL_OFFSET))
            {
                int previousMaxLength = (int)(_nppHelper.GetMarginWidthN(sciPtr, ERROR_DESCRIPTION_MARGIN) / GetMarginLength(sciPtr));
                if (sciPtr == _nppHelper.MainScintilla)
                {
                    _currentPixelFactorMainScintilla = CalculatedPixels(newZoomLevel);
                    if (_lastMainViewAnnotatedFile != string.Empty)
                    {
                        _nppHelper.SetMarginWidthN(sciPtr, ERROR_DESCRIPTION_MARGIN, (int)((double)previousMaxLength * _currentPixelFactorMainScintilla));
                    }
                }
                else
                {
                    _currentPixelFactorSecondScintilla = CalculatedPixels(newZoomLevel);
                    if (_lastSubViewAnnotatedFile != string.Empty)
                    {
                        _nppHelper.SetMarginWidthN(sciPtr, ERROR_DESCRIPTION_MARGIN, (int)((double)previousMaxLength * _currentPixelFactorSecondScintilla));
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
                _nppHelper.SetMarginWidthN(scintilla, ERROR_DESCRIPTION_MARGIN, (int)((double)maxRequiredLength * GetMarginLength(scintilla)));
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
                if (errors != null)
                {
                    int maxCharLenghtForMargin = 0;
                    //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                    var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line).AsParallel();
                    foreach (var errorGroup in aErrorGroupByLines)
                    {
                        _nppHelper.SetMarginStyle(sciPtr, errorGroup.First().LineForScintilla, 1);
                        _nppHelper.SetMarginText(sciPtr, errorGroup.First().LineForScintilla, errorGroup.First().Severity.ToString());
                        int aDescriptionLength = errorGroup.First().Severity.ToString().Length;
                        if(aDescriptionLength > maxCharLenghtForMargin)
                        {
                            maxCharLenghtForMargin = aDescriptionLength;
                        }
                    }
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
        #endregion
    }
}