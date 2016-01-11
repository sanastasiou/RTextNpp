using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RTextNppPlugin.Scintilla.Annotations
{
    internal abstract class ErrorBase : IDisposable
    {
        #region [Data Members]

        protected readonly ISettings _settings                    = null;
        protected readonly INpp _nppHelper                        = null;
        protected bool _areAnnotationEnabled                      = false;
        protected string _lastMainViewAnnotatedFile               = string.Empty;
        protected string _lastSubViewAnnotatedFile                = string.Empty;
        protected bool _disposed                                  = false;
        protected IList<ErrorListViewModel> _currentErrors        = null;
        protected string _workspaceRoot                           = string.Empty;
        protected ILineVisibilityObserver _lineVisibilityObserver = null;
        protected VisibilityInfo _mainVisibilityInfo              = null;
        protected VisibilityInfo _subVisibilityInfo               = null;
        protected string _activeFileMain                          = string.Empty;
        protected string _activeFileSub                           = string.Empty;
        protected IEnumerable _indicatorRangesMain                = null;
        protected IEnumerable _indicatorRangesSub                 = null; 
        private string _drawingFileMain                           = string.Empty;
        private string _drawingFileSub                            = string.Empty;
        private readonly NppData _nppData                         = default(NppData);
        private CancellationTokenSource _mainSciCts               = null;
        private CancellationTokenSource _subSciCts                = null;
        private Task _mainSciDrawingTask                          = null;
        private Task _subSciDrawningTask                          = null;
        private bool _isShutingDown                               = false;

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

        protected bool IsNotepadShutingDown
        {
            get
            {
                return _isShutingDown;
            }
            private set
            {
                _isShutingDown = value;
            }
        }

        #endregion

        #region [Interface]
        protected ErrorBase(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot, ILineVisibilityObserver lineVisibilityObserver)
        {
            _settings                                       = settings;
            _nppHelper                                      = nppHelper;
            _settings.OnSettingChanged                      += OnSettingChanged;
            _nppData                                        = plugin.NppData;
            _areAnnotationEnabled                           = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
            _workspaceRoot                                  = workspaceRoot;
            plugin.BufferActivated                          += OnBufferActivated;
            _lineVisibilityObserver                         = lineVisibilityObserver;
            _lineVisibilityObserver.OnVisibilityInfoUpdated += OnVisibilityInfoUpdated;
            _mainVisibilityInfo                             = plugin.MainVisibilityInfo;
            _subVisibilityInfo                              = plugin.SubVisibilityInfo;
            plugin.OnNotepadShutdown                        += OnNotepadShutdown;
            _activeFileMain                                 = _nppHelper.GetActiveFile(_nppHelper.MainScintilla);
            _activeFileSub                                  = _nppHelper.GetActiveFile(_nppHelper.SecondaryScintilla);
        }

        void OnNotepadShutdown()
        {
            IsNotepadShutingDown = true;
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
                _settings.OnSettingChanged                      -= OnSettingChanged;
                Plugin.Instance.BufferActivated                 -= OnBufferActivated;
                _lineVisibilityObserver.OnVisibilityInfoUpdated -= OnVisibilityInfoUpdated;
                Plugin.Instance.OnNotepadShutdown               -= OnNotepadShutdown;
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
        }

        #region [Abstract]
        protected abstract bool DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr);

        public abstract void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e);

        protected abstract void HideAnnotations(IntPtr scintilla);

        protected abstract void PlaceAnnotations(IntPtr sciPtr, bool waitForTask = false);

        #endregion

        #endregion

        #region [Event Handlers]
        protected virtual object OnVisibilityInfoUpdated(VisibilityInfo info)
        {
            //this event comes before buffer is activated - errors do not match with the file
            SetVisibilityInfo(info);
            return null;
        }

        protected abstract void OnBufferActivated(object source, string file, View view);

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

        protected UpdateAction ValidateErrorList(out ErrorListViewModel errors, string activeViewFile)
        {
            errors = null;

            if (string.IsNullOrEmpty(activeViewFile))
            {
                return UpdateAction.NoAction;
            }

            var activeFileRText = activeViewFile.Replace('\\', '/');

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
            if (_currentErrors != null && _areAnnotationEnabled)
            {
                ErrorListViewModel aErrors = null;
                var aAction = ValidateErrorList(out aErrors, activeViewFile);
                if (aAction != UpdateAction.NoAction)
                {
                    if(!DrawAnnotations(aErrors, sciPtr))
                    {
                        //if draw annotations failed - reset last file
                        activeViewFile = string.Empty;
                    }
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

        protected void PreProcessOnBufferActivatedEvent(string file, View view)
        {
            _activeFileMain = _nppHelper.GetActiveFile(_nppHelper.MainScintilla);
            _activeFileSub  = _nppHelper.GetActiveFile(_nppHelper.SecondaryScintilla);
            SetVisibilityInfo(_lineVisibilityObserver.MainVisibilityInfo);
            SetVisibilityInfo(_lineVisibilityObserver.SubVisibilityInfo);

            if (ErrorList != null)
            {
                if (view == View.Main)
                {
                    string previousAnnotatedFile = _lastMainViewAnnotatedFile;
                    _lastMainViewAnnotatedFile   = file;
                    RefreshErrorsOnBufferActivation(previousAnnotatedFile, ref _lastMainViewAnnotatedFile, _nppHelper.MainScintilla);
                }
                else
                {
                    string previousAnnotatedFile = _lastSubViewAnnotatedFile;
                    _lastSubViewAnnotatedFile    = file;
                    RefreshErrorsOnBufferActivation(previousAnnotatedFile, ref _lastSubViewAnnotatedFile, _nppHelper.SecondaryScintilla);
                }
            }
        }

        protected bool IsWorkspaceFile(string file)
        {
            return (Utilities.FileUtilities.FindWorkspaceRoot(file) + Path.GetExtension(file)).Equals(_workspaceRoot);
        }

        protected VisibilityInfo GetVisibilityInfo(IntPtr sciPtr)
        {
            if(sciPtr == _nppHelper.MainScintilla)
            {
                return _mainVisibilityInfo;
            }
            return _subVisibilityInfo;
        }

        protected void SetVisibilityInfo(VisibilityInfo info)
        {
            if(info.ScintillaHandle == _nppHelper.MainScintilla)
            {
                _mainVisibilityInfo = info;
                return;
            }
            _subVisibilityInfo = info;
        }

        protected void ResetLastAnnotatedFile(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _lastMainViewAnnotatedFile = string.Empty;
                return;
            }
            _lastSubViewAnnotatedFile = string.Empty;
        }

        protected string GetLastAnnotatedFile(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                return _lastMainViewAnnotatedFile;
            }
            return _lastSubViewAnnotatedFile;
        }

        protected bool HasFocus(IntPtr sci)
        {
            if (_nppHelper.MainScintilla == sci)
            {
                return Plugin.Instance.HasMainSciFocus;
            }
            return Plugin.Instance.HasSecondSciFocus;
        }

        protected Task GetDrawingTask(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                return _mainSciDrawingTask;
            }
            return _subSciDrawningTask;
        }

        protected CancellationTokenSource GetCts(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                return _mainSciCts;
            }
            return _subSciCts;
        }

        protected void SetDrawingTask(IntPtr sciPtr, Task task)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _mainSciDrawingTask = task;
            }
            _subSciDrawningTask = task;
        }

        protected void SetCts(IntPtr sciPtr, CancellationTokenSource cts)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _mainSciCts = cts;
            }
            _subSciCts = cts;
        }

        protected string GetActiveFile(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                return _activeFileMain;
            }
            return _activeFileSub;
        }

        protected void SetDrawingFile(IntPtr sciPtr, string file)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _drawingFileMain = file;
                return;
            }
            _drawingFileSub = file;
        }

        protected string GetDrawingFile(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                return _drawingFileMain;
            }
            return _drawingFileSub;
        }

        protected void SetAnnotations<T>(IntPtr sciPtr, T bag) where T : IEnumerable
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _indicatorRangesMain = bag;
                return;
            }
            _indicatorRangesSub = bag;
        }

        protected IEnumerable GetAnnotations(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                return (IEnumerable)_indicatorRangesMain;
            }
            return (IEnumerable)_indicatorRangesSub;
        }

        private void RefreshErrorsOnBufferActivation(string previousAnnotatedFile, ref string currentFile, IntPtr sciPtr)
        {
            if (previousAnnotatedFile != currentFile && !string.IsNullOrEmpty(currentFile))
            {
                if (IsWorkspaceFile(currentFile))
                {
                    Refresh(ref currentFile, sciPtr);
                }
            }
        }
        #endregion
    }
}
