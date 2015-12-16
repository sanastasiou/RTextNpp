using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RTextNppPlugin.Scintilla.Annotations
{
    internal abstract class ErrorBase : IDisposable
    {
        #region [Data Members]
        protected readonly ISettings _settings                      = null;
        protected readonly INpp _nppHelper                          = null;
        protected bool _areAnnotationEnabled                        = false;
        protected string _lastMainViewAnnotatedFile                 = string.Empty;
        protected string _lastSubViewAnnotatedFile                  = string.Empty;
        protected bool _disposed                                    = false;
        private readonly NppData _nppData                           = default(NppData);
        protected IList<ErrorListViewModel> _currentErrors          = null;
        protected string _workspaceRoot                             = string.Empty;
        private DelayedEventHandler<object> _bufferActivatedHandler = null;
        private bool _hasMainScintillaFocus                         = true;
        private bool _hasSecondScintillaFocus                       = true;

        protected enum UpdateAction
        {
            NoAction,
            Delete,
            Update
        }
        #endregion

        #region [Properties]
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
        #endregion

        #region [Interface]
        protected ErrorBase(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot, double bufferActivationDelay)
        {
            _settings                    = settings;
            _nppHelper                   = nppHelper;
            _settings.OnSettingChanged   += OnSettingChanged;
            _nppData                     = plugin.NppData;
            _areAnnotationEnabled        = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
            _workspaceRoot               = workspaceRoot;
            plugin.BufferActivated       += OnBufferActivated;
            _bufferActivatedHandler      = new DelayedEventHandler<object>(null, bufferActivationDelay);
            plugin.ScintillaFocusChanged += OnScintillaFocusChanged;
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

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        

        public void Refresh()
        {
            Refresh(ref _lastMainViewAnnotatedFile, _nppHelper.MainScintilla);
            Refresh(ref _lastSubViewAnnotatedFile, _nppHelper.SecondaryScintilla);
            //ensure that after a new buffer is activated annotations will be redrawn once - in case scintilla didn't have focus
            var aCurrentDoc = _nppHelper.GetCurrentFilePath();
            if (!_lastMainViewAnnotatedFile.Equals(aCurrentDoc))
            {
                _lastMainViewAnnotatedFile = string.Empty;
            }
            if (!_lastSubViewAnnotatedFile.Equals(aCurrentDoc))
            {
                _lastSubViewAnnotatedFile = string.Empty;
            }
        }

        #region [Abstract]
        protected abstract void DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr);

        public abstract void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e);

        protected abstract void HideAnnotations(IntPtr scintilla);

        protected abstract object OnBufferActivated(string file);
        #endregion

        #endregion

        #region [Event Handlers]
        public void OnBufferActivated(object source, string file)
        {
            _bufferActivatedHandler.TriggerHandler(new ActionWrapper<object, string>(OnBufferActivated, file));
        }

        private void OnScintillaFocusChanged(IntPtr sciPtr, bool hasFocus)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _hasMainScintillaFocus = hasFocus;
            }
            else if (sciPtr == _nppHelper.SecondaryScintilla)
            {
                _hasSecondScintillaFocus = hasFocus;
            }
        }
        #endregion

        #region [Helpers]
        abstract protected Constants.StyleId ConvertSeverityToStyleId(ErrorItemViewModel.SeverityType severity);

        protected string FindActiveFile(IntPtr sciPtr)
        {
            //get opened files
            var openedFiles = _nppHelper.GetOpenFiles(sciPtr);
            //check current doc index
            int viewIndex   = _nppHelper.CurrentDocIndex(sciPtr);

            string activeFile = string.Empty;

            if (viewIndex != Constants.Scintilla.VIEW_NOT_ACTIVE)
            {
                var aTempFile = openedFiles[viewIndex];
                if(IsWorkspaceFile(aTempFile))
                {
                    return aTempFile;
                }
            }
            return string.Empty;
        }

        protected bool HasSciFocus(IntPtr sciPtr)
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

        protected UpdateAction ValidateErrorList(out ErrorListViewModel errors, IntPtr sciPtr, ref string activeViewFile)
        {
            errors = null;
            activeViewFile = string.Empty;

            //get opened files
            var openedFiles = _nppHelper.GetOpenFiles(sciPtr);
            //check current doc index
            int viewIndex = _nppHelper.CurrentDocIndex(sciPtr);

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

        protected void Refresh(ref string activeViewFile, IntPtr sciPtr)
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

        protected void ProcessSettingChanged()
        {
            if (_areAnnotationEnabled)
            {
                Refresh(ref _lastMainViewAnnotatedFile, _nppHelper.MainScintilla);
                Refresh(ref _lastSubViewAnnotatedFile, _nppHelper.SecondaryScintilla);
            }
            else
            {
                HideAnnotations(_nppHelper.MainScintilla);
                HideAnnotations(_nppHelper.SecondaryScintilla);
                _lastMainViewAnnotatedFile = _lastSubViewAnnotatedFile = string.Empty;
            }
        }

        protected void PreProcessOnBufferActivatedEvent()
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
                    if (IsWorkspaceFile(_lastMainViewAnnotatedFile))
                    {
                        Refresh(ref _lastMainViewAnnotatedFile, _nppHelper.MainScintilla);
                    }
                }
                if (previousAnnotatedSubFile != _lastSubViewAnnotatedFile && !string.IsNullOrEmpty(_lastSubViewAnnotatedFile))
                {
                    if (IsWorkspaceFile(_lastSubViewAnnotatedFile))
                    {
                        Refresh(ref _lastSubViewAnnotatedFile, _nppHelper.SecondaryScintilla);
                    }
                }
            }            
        }

        protected bool IsWorkspaceFile(string file)
        {
            return (Utilities.FileUtilities.FindWorkspaceRoot(file) + Path.GetExtension(file)).Equals(_workspaceRoot);
        }
        #endregion
    }
}
