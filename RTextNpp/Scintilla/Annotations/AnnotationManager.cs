using RTextNppPlugin.DllExport;
using RTextNppPlugin.RText;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RTextNppPlugin.Scintilla.Annotations
{
    class AnnotationManager : IError, IDisposable
    {
        #region [Data Members]
        private readonly ISettings _settings                        = null;
        private readonly INpp _nppHelper                            = null;
        private const Settings.RTextNppSettings SETTING             = Settings.RTextNppSettings.EnableErrorAnnotations;
        private bool _areAnnotationEnabled                          = false;
        private string _lastMainViewAnnotatedFile                   = string.Empty;
        private string _lastSubViewAnnotatedFile                    = string.Empty;
        private bool _disposed                                      = false;
        private readonly NppData _nppData                           = default(NppData);
        private IList<ErrorListViewModel> _currentErrors            = null;
        private string _workspaceRoot                               = string.Empty;
        private DelayedEventHandler<object> _bufferActivatedHandler = null;
        private bool _hasMainScintillaFocus                         = true;
        private bool _hasSecondScintillaFocus                       = true;
        #endregion

        #region [Interface]
        internal AnnotationManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot)
        {
            _settings                    = settings;
            _nppHelper                   = nppHelper;
            _settings.OnSettingChanged   += OnSettingChanged;
            _nppData                     = plugin.NppData;
            _areAnnotationEnabled        = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
            _workspaceRoot               = workspaceRoot;
            plugin.BufferActivated       += OnBufferActivated;
            _bufferActivatedHandler      = new DelayedEventHandler<object>(null, 100);
            plugin.ScintillaFocusChanged += OnScintillaFocusChanged;
        }

        public void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
            }
            if (_areAnnotationEnabled)
            {
                RefreshAnnotations(ref _lastMainViewAnnotatedFile, _nppHelper.MainScintilla);
                RefreshAnnotations(ref _lastSubViewAnnotatedFile, _nppHelper.SecondaryScintilla);
            }
            else
            {
                HideAnnotations(_nppHelper.MainScintilla);
                HideAnnotations(_nppHelper.SecondaryScintilla);
                _lastMainViewAnnotatedFile = _lastSubViewAnnotatedFile = string.Empty;
            }
        }

        public void OnBufferActivated(object source, string file)
        {
            _bufferActivatedHandler.TriggerHandler(new ActionWrapper<object, string>(OnBufferActivated, file));
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IList<ErrorListViewModel> ErrorList
        { 
            get
            {
                return _currentErrors;
            }
            set
            {
                if (value != null)
                {
                    _currentErrors = new List<ErrorListViewModel>(value);
                }
            }
        }

        public void RefreshAnnotations()
        {
            RefreshAnnotations(ref _lastMainViewAnnotatedFile, _nppHelper.MainScintilla);
            RefreshAnnotations(ref _lastSubViewAnnotatedFile, _nppHelper.SecondaryScintilla);
            //ensure that after a new buffer is activated annotations will be redrawn once - in case scintilla didn't have focus
            var aCurrentDoc = _nppHelper.GetCurrentFilePath();
            if(!_lastMainViewAnnotatedFile.Equals(aCurrentDoc))
            {
                _lastMainViewAnnotatedFile = string.Empty;
            }
            if (!_lastSubViewAnnotatedFile.Equals(aCurrentDoc))
            {
                _lastSubViewAnnotatedFile = string.Empty;
            }
        }
        #endregion

        #region [Helpers]

        #region [Event Handlers]
        private void OnScintillaFocusChanged(IntPtr sciPtr, bool hasFocus)
        {
            if(sciPtr == _nppHelper.MainScintilla)
            {
                _hasMainScintillaFocus = hasFocus;
            }
            else if(sciPtr == _nppHelper.SecondaryScintilla)
            {
                _hasSecondScintillaFocus = hasFocus;
            }
        }
        #endregion

        private object OnBufferActivated(string file)
        {
            //only update if connector is already loaded
            if (ErrorList != null)
            {
                string previousAnnotatedMainFile = _lastMainViewAnnotatedFile;
                string previousAnnotatedSubFile  = _lastSubViewAnnotatedFile;
                _lastMainViewAnnotatedFile       = FindActiveFile(_nppHelper.MainScintilla);
                _lastSubViewAnnotatedFile        = FindActiveFile(_nppHelper.SecondaryScintilla);
                if (previousAnnotatedMainFile != _lastMainViewAnnotatedFile && !string.IsNullOrEmpty(_lastMainViewAnnotatedFile))
                {
                    RefreshAnnotations(ref _lastMainViewAnnotatedFile, _nppHelper.MainScintilla);
                }
                if (previousAnnotatedSubFile != _lastSubViewAnnotatedFile && !string.IsNullOrEmpty(_lastSubViewAnnotatedFile))
                {
                    RefreshAnnotations(ref _lastSubViewAnnotatedFile, _nppHelper.SecondaryScintilla);
                }
            }
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

        private void RefreshAnnotations(ref string activeViewFile, IntPtr sciPtr)
        {
            //ensure model is loaded
            if (_currentErrors != null && HasSciFocus(sciPtr))
            {
                ErrorListViewModel aErrors = null;
                var aAction = ValidateErrorList(out aErrors, sciPtr, ref activeViewFile);
                if (aAction != UpdateAction.NoAction)
                {
                    DrawAnnotations(aErrors, sciPtr);
                }
            }
            else
            {
                activeViewFile = string.Empty;
            }
        }

        private void ShowAnnotations(IntPtr scintilla)
        {
            if (_areAnnotationEnabled)
            {
                _nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.BOXED_ANNOTATION_STYLE);
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
                _settings.OnSettingChanged            -= OnSettingChanged;
                Plugin.Instance.BufferActivated       -= OnBufferActivated;
                Plugin.Instance.ScintillaFocusChanged -= OnScintillaFocusChanged;
            }
            _disposed = true;
        }

        private void HideAnnotations(IntPtr scintilla)
        {
            var openFiles = _nppHelper.GetOpenFiles(scintilla);
            var docIndex  = _nppHelper.CurrentDocIndex(scintilla);
            if(Utilities.FileUtilities.IsRTextFile(openFiles[docIndex], _settings, _nppHelper))
            {
                if((Utilities.FileUtilities.FindWorkspaceRoot(openFiles[docIndex]) + Path.GetExtension(openFiles[docIndex])).Equals(_workspaceRoot))
                {
                    _nppHelper.ClearAllAnnotations(scintilla);
                    _nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.HIDDEN_ANNOTATION_STYLE);
                }
            }
        }

        private enum UpdateAction
        {
            NoAction,
            Delete,
            Update
        }

        private UpdateAction ValidateErrorList(out ErrorListViewModel errors, IntPtr sciPtr, ref string activeViewFile)
        {
            errors            = null;
            activeViewFile    = string.Empty;

            //get opened files
            var openedFiles   = _nppHelper.GetOpenFiles(sciPtr);
            //check current doc index
            int viewIndex     = _nppHelper.CurrentDocIndex(sciPtr);

            string activeFile = FindActiveFile(sciPtr);

            if (string.IsNullOrEmpty(activeFile))
            {
                return UpdateAction.NoAction;
            }

            activeViewFile = activeFile;

            var activeFileRText = activeFile.Replace('\\', '/');

            //if we are here, it means workspaces match - check if files has errors
            errors = _currentErrors.FirstOrDefault(x => x.FilePath.Equals(activeFileRText, StringComparison.InvariantCultureIgnoreCase));

            if (errors == null || errors.ErrorList.Count == 0)
            {
                return UpdateAction.Delete;
            }

            return UpdateAction.Update;
        }

        private void DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr)
        {
            HideAnnotations(sciPtr);
            try
            {
                if (errors != null)
                {
                    //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                    var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line).AsParallel();
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
                        //npp offset for line todo - add multiple styles
                        _nppHelper.SetAnnotationStyle((errorGroup.First().Line - 1), Constants.StyleId.ANNOTATION_ERROR);
                        _nppHelper.AddAnnotation((errorGroup.First().Line - 1), aErrorDescription);
                    }
                    ShowAnnotations(sciPtr);
                }
            }
            catch (Exception)
            {
                Trace.WriteLine("DrawAnnotations failed.");
            }
        }

        private string FindActiveFile(IntPtr sciPtr)
        {
            //get opened files
            var openedFiles = _nppHelper.GetOpenFiles(sciPtr);
            //check current doc index
            int viewIndex   = _nppHelper.CurrentDocIndex(sciPtr);

            string activeFile = string.Empty;

            if (viewIndex != Constants.Scintilla.VIEW_NOT_ACTIVE)
            {
                var aTempFile = openedFiles[viewIndex];
                var viewWorkspace = Utilities.FileUtilities.FindWorkspaceRoot(aTempFile) + Path.GetExtension(aTempFile);
                if(viewWorkspace.Equals(_workspaceRoot))
                {
                    return aTempFile;
                }
            }
            return string.Empty;
        }

        private bool HasSciFocus(IntPtr sciPtr)
        {
            if(sciPtr == _nppHelper.MainScintilla)
            {
                return _hasMainScintillaFocus;
            }
            else if(sciPtr == _nppHelper.SecondaryScintilla)
            {
                return _hasSecondScintillaFocus;
            }
            return false;
        }
        #endregion
    }
}
