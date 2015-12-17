using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RTextNppPlugin.Scintilla.Annotations
{
    internal class AnnotationManager : ErrorBase, IError
    {
        #region [Data Members]
        private const Settings.RTextNppSettings SETTING = Settings.RTextNppSettings.EnableErrorAnnotations;
        #endregion

        #region [Interface]
        internal AnnotationManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot,  ILineVisibilityObserver lineVisibilityObserver, double updateDelay = Constants.Scintilla.ANNOTATIONS_UPDATE_DELAY) :
            base(settings, nppHelper, plugin, workspaceRoot, lineVisibilityObserver, updateDelay)
        {
            _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
                ProcessSettingChanged();
            }
        }

        protected override Constants.StyleId ConvertSeverityToStyleId(ErrorItemViewModel.SeverityType severity)
        {
            switch (severity)
            {
                case ErrorItemViewModel.SeverityType.Debug:
                    return Constants.StyleId.ANNOTATION_DEBUG;
                case ErrorItemViewModel.SeverityType.Info:
                    return Constants.StyleId.ANNOTATION_INFO;
                case ErrorItemViewModel.SeverityType.Warning:
                    return Constants.StyleId.ANNOTATION_WARNING;
                case ErrorItemViewModel.SeverityType.Error:
                    return Constants.StyleId.ANNOTATION_ERROR;
                case ErrorItemViewModel.SeverityType.Fatal:
                    return Constants.StyleId.ANNOTATION_FATAL_ERROR;
                default:
                    return Constants.StyleId.ANNOTATION_ERROR;
            }
        }

        #endregion

        #region [Helpers]

        protected override object OnBufferActivated(string file)
        {
            PreProcessOnBufferActivatedEvent();
            if(!Utilities.FileUtilities.IsRTextFile(file, _settings, _nppHelper))
            {
                //remove annotations from the view which this file belongs to
                var scintilla = _nppHelper.FindScintillaFromFilepath(file);
                _nppHelper.ClearAllAnnotations(scintilla);
                _nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.HIDDEN_ANNOTATION_STYLE);
                if(scintilla == _nppHelper.MainScintilla)
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

        private void ShowAnnotations(IntPtr scintilla)
        {
            if (_areAnnotationEnabled)
            {
                _nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.BOXED_ANNOTATION_STYLE);
            }
        }

        protected override void HideAnnotations(IntPtr scintilla)
        {
            var openFiles = _nppHelper.GetOpenFiles(scintilla);
            var docIndex  = _nppHelper.CurrentDocIndex(scintilla);
            if(docIndex != Constants.Scintilla.VIEW_NOT_ACTIVE && Utilities.FileUtilities.IsRTextFile(openFiles[docIndex], _settings, _nppHelper))
            {
                if (IsWorkspaceFile(openFiles[docIndex]))
                {
                    _nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.HIDDEN_ANNOTATION_STYLE);
                    _nppHelper.ClearAllAnnotations(scintilla);
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
                    //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                    var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line);
                    foreach (var errorGroup in aErrorGroupByLines)
                    {
                        StringBuilder aErrorDescription = new StringBuilder(errorGroup.Count() * 50);
                        int aErrorCounter = 0;
                        foreach (var error in errorGroup)
                        {
                            aErrorDescription.AppendFormat("{0} : {2}", error.Severity, error.Line, error.Message);
                            if (++aErrorCounter < errorGroup.Count())
                            {
                                aErrorDescription.Append("\n");
                            }
                        }
                        //npp offset for line todo - add multiple styles
                        _nppHelper.SetAnnotationStyle((errorGroup.First().LineForScintilla), (int)Constants.StyleId.ANNOTATION_ERROR);
                        _nppHelper.AddAnnotation((errorGroup.First().LineForScintilla), aErrorDescription);
                    }
                    ShowAnnotations(sciPtr);
                }
            }
            catch (Exception)
            {
                Trace.WriteLine("DrawAnnotations failed.");
            }
        }
        #endregion
    }
}
