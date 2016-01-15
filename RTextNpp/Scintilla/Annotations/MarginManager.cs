﻿using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;

namespace RTextNppPlugin.Scintilla.Annotations
{
    class MarginManager : ErrorBase, IError
    {
        #region [Data Members]
        private const Settings.RTextNppSettings SETTING   = Settings.RTextNppSettings.EnableErrorMarkers;
        private double _currentPixelFactorMainScintilla   = 0.0;
        private double _currentPixelFactorSecondScintilla = 0.0;
        private const double PIXEL_ZOOM_FACTOR            = 24.0;
        private const int ERROR_DESCRIPTION_MARGIN        = Constants.Scintilla.SC_MAX_MARGIN - 1;
        private const double MAX_ZOOM_LEVEL               = 30.0;
        private const double ZOOM_LEVEL_OFFSET            = 10.0;
        private int _maxCharLengthMainSci                 = 0;
        private int _maxCharLengthSubSci                  = 0;
        private int _lineNumberStyleBackground            = int.MinValue;
        #endregion

        #region [Interface]

        internal MarginManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot, ILineVisibilityObserver lineVisibilityObserver, double updateDelay = Constants.Scintilla.ANNOTATIONS_UPDATE_DELAY) :
            base(settings, nppHelper, plugin, workspaceRoot, lineVisibilityObserver)
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

            NormalizeMarginsBackground();

            plugin.ScintillaUiPainted += OnScintillaUiPainted;

            HideAnnotations(_nppHelper.MainScintilla);
            HideAnnotations(_nppHelper.SecondaryScintilla);
        }

        void OnScintillaUiPainted()
        {
            NormalizeMarginsBackground();
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
                ProcessSettingChanged();
            }
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

        protected override object OnVisibilityInfoUpdated(VisibilityInfo info)
        {
            if (info != GetVisibilityInfo(info.ScintillaHandle))
            {
                //this event comes before buffer is activated - errors do not match with the file
                base.OnVisibilityInfoUpdated(info);
                //if (IsWorkspaceFile(info.File))
                //{
                //    //update current annotations
                //    PlaceAnnotations(info.ScintillaHandle, true);
                //}
            }
            return null;
        }

        #endregion

        #region [Event Handlers]

        void OnScintillaZoomChanged(IntPtr sciPtr, int newZoomLevel)
        {
            if (newZoomLevel > (-ZOOM_LEVEL_OFFSET))
            {
                int previousMaxLength = GetMaxCharLength(sciPtr);
                var openedFiles       = _nppHelper.GetOpenFiles(sciPtr);
                var currentFile       = _nppHelper.GetCurrentFilePath();
                if (openedFiles.Contains(currentFile) && IsWorkspaceFile(currentFile))
                {
                    if (sciPtr == _nppHelper.MainScintilla)
                    {
                        _currentPixelFactorMainScintilla = CalculatedPixels(newZoomLevel);
                        if (_activeFileMain != string.Empty)
                        {
                            if (_areAnnotationEnabled)
                            {
                                _nppHelper.SetMarginWidthN(sciPtr, ERROR_DESCRIPTION_MARGIN, (int)Math.Ceiling(((double)previousMaxLength * _currentPixelFactorMainScintilla)));
                            }
                        }
                    }
                    else
                    {
                        _currentPixelFactorSecondScintilla = CalculatedPixels(newZoomLevel);
                        if (_activeFileSub != string.Empty)
                        {
                            if (_areAnnotationEnabled)
                            {
                                _nppHelper.SetMarginWidthN(sciPtr, ERROR_DESCRIPTION_MARGIN, (int)Math.Ceiling(((double)previousMaxLength * _currentPixelFactorSecondScintilla)));
                            }
                        }
                    }
                }
            }
        }        

        #endregion

        #region [Helpers]

        protected override void OnBufferActivated(object source, string file, View view)
        {
            PreProcessOnBufferActivatedEvent(file, view);
            if (!Utilities.FileUtilities.IsRTextFile(file, _settings, _nppHelper) || (ErrorList == null && IsWorkspaceFile(file)))
            {
                //remove annotations from the view which this file belongs to
                var scintilla = _nppHelper.ScintillaFromView(view);
                _nppHelper.ClearAllTextMargins(scintilla);
                _nppHelper.SetMarginWidthN(scintilla, ERROR_DESCRIPTION_MARGIN, 0);
                if (scintilla == _nppHelper.MainScintilla)
                {
                    _activeFileMain = string.Empty;
                }
                else
                {
                    _activeFileSub = string.Empty;
                }
            }
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
                if (IsWorkspaceFile(openFiles[docIndex]))
                {
                    _nppHelper.SetMarginWidthN(scintilla, ERROR_DESCRIPTION_MARGIN, 0);
                    _nppHelper.ClearAllTextMargins(scintilla);
                }
            }
        }

        protected override bool DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr)
        {
            HideAnnotations(sciPtr);
            try
            {
                PlaceIndicatorsRanges(sciPtr);
            }
            catch (Exception)
            {
                Trace.WriteLine("Draw margins failed.");
            }
            return true;
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

        private void NormalizeMarginsBackground()
        {
            if (_areAnnotationEnabled)
            {
                _lineNumberStyleBackground = _nppHelper.GetStyleBackground(_nppHelper.MainScintilla, (int)SciMsg.STYLE_LINENUMBER);

                //initialize style backgrounds for all margin styles
                for (var i = Constants.StyleId.MARGIN_DEBUG; i < Constants.StyleId.MARGIN_FATAL_ERROR; ++i)
                {
                    if (_nppHelper.GetStyleBackground(_nppHelper.MainScintilla, (int)i) != _lineNumberStyleBackground)
                    {
                        _nppHelper.SetStyleBackground(_nppHelper.MainScintilla, (int)i, _lineNumberStyleBackground);
                    }
                    if (_nppHelper.GetStyleBackground(_nppHelper.SecondaryScintilla, (int)i) != _lineNumberStyleBackground)
                    {
                        _nppHelper.SetStyleBackground(_nppHelper.SecondaryScintilla, (int)i, _lineNumberStyleBackground);
                    }
                }
            }
        }

        private void PlaceIndicatorsRanges(IntPtr sciPtr)
        {
            var aActiveFile     = _nppHelper.GetActiveFile(sciPtr).Replace("\\\\", "/");
            var aVisibilityInfo = GetVisibilityInfo(sciPtr);
            if (ErrorList != null && ErrorList.Count != 0)
            {
                var aFileErrorList = (from lists in ErrorList
                                     where lists.FilePath.Equals(aActiveFile, StringComparison.InvariantCultureIgnoreCase)
                                     select lists).FirstOrDefault();
                if (aFileErrorList != null)
                {
                    //get only error margins which belong to visible lines
                    var aVisibleMargins = from error in aFileErrorList.ErrorList
                                         where error.Line >= aVisibilityInfo.FirstLine && error.Line <= aVisibilityInfo.LastLine
                                         select error;

                    var aRanges = aVisibleMargins.OrderBy(x => x.Line).GroupBy( x=> x.Line).ToArray();
                    int maxCharLenghtForMargin = 0;
                    for (int i = 0; i < aRanges.Count(); ++i)
                    {
                        _nppHelper.SetMarginStyle(sciPtr, aRanges[i].First().LineForScintilla, (int)ConvertSeverityToStyleId(aRanges[i].First().Severity));
                        _nppHelper.SetMarginText(sciPtr, aRanges[i].First().LineForScintilla, aRanges[i].First().Severity.ToString());
                        int aDescriptionLength = aRanges[i].First().Severity.ToString().Length;
                        if(aDescriptionLength > maxCharLenghtForMargin)
                        {
                            maxCharLenghtForMargin = aDescriptionLength;
                        }
                    }
                    SetMaxCharLength(sciPtr, maxCharLenghtForMargin);
                    ShowAnnotations(sciPtr, maxCharLenghtForMargin);
                }
            }
        }

        protected override void PlaceAnnotations(IntPtr sciPtr, bool waitForTask = false)
        {
            if (!IsNotepadShutingDown)
            {
            }
        }
        #endregion
    }
}