using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Linq;
using System.Text;

namespace RTextNppPlugin.Scintilla.Annotations
{
    class AnnotationManager : IError, IDisposable
    {
        #region [Data Members]
        private readonly ISettings _settings            = null;
        private readonly INpp _nppHelper                = null;
        private const Settings.RTextNppSettings SETTING = Settings.RTextNppSettings.EnableErrorAnnotations;
        private bool _areAnnotationEnabled              = false;
        private ErrorListViewModel _model               = null;
        private string _lastAnnotatedFile               = string.Empty;
        private bool _disposed                          = false;
        private readonly NppData _nppData               = default(NppData);
        #endregion

        #region [Interface]
        internal AnnotationManager(ISettings settings, INpp nppHelper, NppData nppData)
        {
            _settings                  = settings;
            _nppHelper                 = nppHelper;
            _settings.OnSettingChanged += OnSettingChanged;
            _nppData                   = nppData;
            _areAnnotationEnabled      = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
        }

        public void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
            }
            if (_areAnnotationEnabled)
            {
                RefreshAnnotations();
            }
            else
            {
                RemoveErrors(_nppHelper.MainScintilla);
                RemoveErrors(_nppHelper.SecondaryScintilla);
            }
        }

        public void AddErrors(ViewModels.ErrorListViewModel model, IntPtr scintilla)
        {
            _model = model;
            if (_areAnnotationEnabled)
            {
                RefreshAnnotations();
            }
        }

        public void OnBufferActivated(string file)
        {
            if(_lastAnnotatedFile != file)
            {
                //RemoveErrors();
            }
            _lastAnnotatedFile = file;
            RefreshAnnotations();

        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region [Helpers]
        private void RefreshAnnotations()
        {
            //RemoveErrors();
            if(_model != null)
            {
                //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                var aErrorGroupByLines = _model.ErrorList.GroupBy(x => x.Line);
                foreach (var errorGroup in aErrorGroupByLines)
                {
                    StringBuilder aErrorDescription = new StringBuilder(errorGroup.Count() * 50);
                    int aErrorCounter = 0;
                    foreach (var error in errorGroup)
                    {
                        aErrorDescription.AppendFormat("{0} at line : {1} - {2}", error.Severity, error.Line, error.Message);
                        if (++aErrorCounter < errorGroup.Count())
                        {
                            aErrorDescription.Append("\n");
                        }
                    }
                    //npp offset for line
                    _nppHelper.SetAnnotationStyle((errorGroup.First().Line - 1), Constants.StyleId.ANNOTATION_ERROR);
                    _nppHelper.AddAnnotation((errorGroup.First().Line - 1), aErrorDescription);
                }
            }
            ShowAnnotations();
        }

        private void ShowAnnotations()
        {
            if (_areAnnotationEnabled)
            {
                _nppHelper.SetAnnotationVisible(_nppHelper.MainScintilla, Constants.BOXED_ANNOTATION_STYLE);
                _nppHelper.SetAnnotationVisible(_nppHelper.SecondaryScintilla, Constants.BOXED_ANNOTATION_STYLE);
            }
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                _settings.OnSettingChanged -= OnSettingChanged;
            }
            _disposed = true;
        }

        private void RemoveErrors(IntPtr scintilla)
        {
            _nppHelper.SetAnnotationVisible(scintilla, Constants.HIDDEN_ANNOTATION_STYLE);
            _nppHelper.ClearAllAnnotations(scintilla);
        }
        #endregion
    }
}
