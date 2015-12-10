using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

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
        protected readonly NppData _nppData                         = default(NppData);
        protected IList<ErrorListViewModel> _currentErrors          = null;
        protected string _workspaceRoot                             = string.Empty;
        private DelayedEventHandler<object> _bufferActivatedHandler = null;
        protected bool _hasMainScintillaFocus                       = true;
        protected bool _hasSecondScintillaFocus                     = true;

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

        public abstract void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e);

        protected abstract object OnBufferActivated(string file);

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
                var viewWorkspace = Utilities.FileUtilities.FindWorkspaceRoot(aTempFile) + Path.GetExtension(aTempFile);
                if(viewWorkspace.Equals(_workspaceRoot))
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
        #endregion
    }
}
