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
        private readonly ISettings _settings             = null;
        private readonly INpp _nppHelper                 = null;
        private const Settings.RTextNppSettings SETTING  = Settings.RTextNppSettings.EnableErrorAnnotations;
        private bool _areAnnotationEnabled               = false;
        private string _lastMainViewAnnotatedFile        = string.Empty;
        private string _lastSubViewAnnotatedFile         = string.Empty;
        private bool _disposed                           = false;
        private readonly NppData _nppData                = default(NppData);
        private IList<ErrorListViewModel> _currentErrors = null;
        private CancellationTokenSource _mainSciCts      = null;
        private CancellationTokenSource _subSciCts       = null;
        private string _workspaceRoot                    = string.Empty;
        private Task _mainViewTask                       = null;
        private Task _subViewTask                        = null;
        #endregion

        #region [Interface]
        internal AnnotationManager(ISettings settings, INpp nppHelper, NppData nppData, string workspaceRoot)
        {
            _settings                  = settings;
            _nppHelper                 = nppHelper;
            _settings.OnSettingChanged += OnSettingChanged;
            _nppData                   = nppData;
            _areAnnotationEnabled      = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
            _workspaceRoot             = workspaceRoot;
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
                HideAnnotations(_nppHelper.MainScintilla);
                HideAnnotations(_nppHelper.SecondaryScintilla);
            }
        }

        public void OnBufferActivated(string file)
        {
            //if(_lastAnnotatedFile != file)
            //{
            //    //RemoveErrors();
            //}
            //_lastAnnotatedFile = file;
            RefreshAnnotations();

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
            ErrorListViewModel aPrimaryErrors   = null;
            ErrorListViewModel aSecondaryErrors = null;
            var aPrimaryViewAction   = ValidateErrorList(out aPrimaryErrors, NppMsg.PRIMARY_VIEW, NppMsg.MAIN_VIEW);
            if(aPrimaryViewAction != UpdateAction.NoAction)
            {
                DrawAnnotations(aPrimaryErrors, _nppHelper.MainScintilla, ref _mainViewTask, ref _mainSciCts);
            }

            var aSecondaryViewAction = ValidateErrorList(out aSecondaryErrors, NppMsg.SECOND_VIEW, NppMsg.SUB_VIEW);
            if (aSecondaryViewAction != UpdateAction.NoAction)
            {
                DrawAnnotations(aSecondaryErrors, _nppHelper.SecondaryScintilla, ref _subViewTask, ref _subSciCts);
            }
        }

        #endregion

        #region [Helpers]
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
                _settings.OnSettingChanged -= OnSettingChanged;
            }
            _disposed = true;
        }

        private void HideAnnotations(IntPtr scintilla)
        {
            _nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.HIDDEN_ANNOTATION_STYLE);
            _nppHelper.ClearAllAnnotations(scintilla);
        }

        private enum UpdateAction
        {
            NoAction,
            Delete,
            Update
        }

        private UpdateAction ValidateErrorList(out ErrorListViewModel errors, NppMsg openFilesView, NppMsg docIndexView)
        {
            errors = null;
            //get opened files
            var openedFiles = _nppHelper.GetOpenFiles(openFilesView);
            //check current doc index
            int viewIndex   = _nppHelper.CurrentDocIndex(docIndexView);

            string activeFile = string.Empty;

            if (viewIndex != Constants.Scintilla.VIEW_NOT_ACTIVE)
            {
                activeFile        = openedFiles[viewIndex];
                var viewWorkspace = Utilities.FileUtilities.FindWorkspaceRoot(activeFile) + Path.GetExtension(activeFile);
                if(!viewWorkspace.Equals(_workspaceRoot))
                {
                    return UpdateAction.NoAction;
                }
            }
            else
            {
                return UpdateAction.NoAction;
            }
            var activeFileRText = activeFile.Replace('\\', '/');

            //if we are here, it means workspaces match - check if files has errors
            errors = _currentErrors.FirstOrDefault(x => x.FilePath.Equals(activeFileRText, StringComparison.InvariantCultureIgnoreCase));
            if (errors == null || errors.ErrorList.Count == 0)
            {
                return UpdateAction.Delete;
            }

            return UpdateAction.Update;
        }

        private void DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr, ref Task runningTask, ref CancellationTokenSource cts)
        {
            //always hide annotations before redrawing
            HideAnnotations(sciPtr);
            if (errors == null || errors.ErrorList.Count == 0)
            {
                //current view has no errors or is not part of active namespace
                //cancel any pending task and wait for it to finish execution
                if (runningTask != null && !runningTask.IsCanceled)
                {
                    cts.Cancel();
                    runningTask = null;
                    cts         = null;
                }
            }
            else
            {
                try
                {
                    //start async task to draw annotations and return
                    cts = new CancellationTokenSource();
                    runningTask = Task.Run(() =>
                    {
                        //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                        var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line).AsParallel();
                        Parallel.ForEach(aErrorGroupByLines, (errorGroup) =>
                        {
                            Trace.WriteLine(String.Format("DrawAnnotations inside : {0} sciPtr", sciPtr));
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
                        });
                    }, cts.Token);
                    var showAnnotationTask = runningTask.ContinueWith(r => { if (r.IsCompleted) ShowAnnotations(sciPtr); }, TaskContinuationOptions.OnlyOnRanToCompletion);
                }
                catch (OperationCanceledException)
                {
                    Trace.WriteLine("DrawAnnotations canceled.");
                }
                catch (Exception)
                {
                    Trace.WriteLine("DrawAnnotations failed.");
                }
            }
        }
        #endregion
    }
}
