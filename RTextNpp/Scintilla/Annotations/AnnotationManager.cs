using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RTextNppPlugin.Scintilla.Annotations
{
    internal class AnnotationManager : ErrorBase, IError
    {
        #region [Data Members]
        private const Settings.RTextNppSettings SETTING = Settings.RTextNppSettings.EnableErrorAnnotations;
        #endregion

        #region [Interface]
        internal AnnotationManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot,  ILineVisibilityObserver lineVisibilityObserver) :
            base(settings, nppHelper, plugin, workspaceRoot, lineVisibilityObserver)
        {
            _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                bool aNewSettingValue = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorAnnotations);
                if (aNewSettingValue != _areAnnotationEnabled)
                {
                    ProcessSettingChanged(aNewSettingValue);
                }
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

        protected override void OnVisibilityInfoUpdated(VisibilityInfo info, IntPtr sciPtr)
        {
            base.OnVisibilityInfoUpdated(info, sciPtr);
            if (IsWorkspaceFile(info.File) && _areAnnotationEnabled)
            {
                //update current annotations
                PlaceAnnotations(info.ScintillaHandle, true);
            }
        }

        #endregion

        #region [Helpers]

        protected override void OnBufferActivated(object source, string file, View view)
        {
            PreProcessOnBufferActivatedEvent(file, view);
            if(!Utilities.FileUtilities.IsRTextFile(file, _settings, _nppHelper))
            {
                //remove annotations from the view which this file belongs to
                var scintilla = _nppHelper.ScintillaFromView(view);
                HideAnnotations(scintilla);
                SetActiveFile(scintilla, string.Empty);
            }
        }

        protected override void HideAnnotations(IntPtr scintilla)
        {
            _nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.HIDDEN_ANNOTATION_STYLE);
            _nppHelper.ClearAllAnnotations(scintilla);
        }

        protected override bool DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr)
        {
            bool aSuccess = true;
            if (!IsNotepadShutingDown && _areAnnotationEnabled)
            {
                try
                {
                    //cancel any pending task
                    var task = GetDrawingTask(sciPtr);
                    var cts = GetCts(sciPtr);
                    if (task != null && !(task.IsCanceled || task.IsCompleted))
                    {
                        cts.Cancel();
                        try
                        {
                            task.Wait();
                        }
                        catch (AggregateException ex)
                        {
                            ex.Handle(ae => true);
                        }
                        SetAnnotations<IEnumerable>(sciPtr, null);
                    }
                    HideAnnotations(sciPtr);
                    //start new task
                    var newCts                                                   = new CancellationTokenSource();
                    ConcurrentBag<Tuple<int, StringBuilder, byte[]>> annotations = new ConcurrentBag<Tuple<int, StringBuilder, byte[]>>();
                    if(errors == null || errors.ErrorList == null || errors.ErrorList.Count == 0)
                    {
                        SetAnnotations(sciPtr, annotations);
                        return true;
                    }
                    string activeFile = errors.FilePath.Replace("/", "\\");
                    SetCts(sciPtr, newCts);
                    SetDrawingFile(sciPtr, activeFile);
                    var newTask = Task.Factory.StartNew(() =>
                    {
                        var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line);
                        //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                        Parallel.ForEach( aErrorGroupByLines,
                                          new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : Environment.ProcessorCount },
                                          (IGrouping<int, ErrorItemViewModel> aErrorGroup, ParallelLoopState state) =>
                        {
                            StringBuilder aErrorDescription = new StringBuilder(aErrorGroup.Count() * 50); // Allocate some initial capacity to avoid multiple reallocations
                            int aErrorCounter               = 0;
                            int aStyleOffset                = 0;
                            List<byte> aStyles              = new List<byte>(aErrorGroup.Count() * 50); // Allocate some initial capacity to avoid multiple reallocations
                            foreach (var error in aErrorGroup)
                            {
                                bool hasActiveFileChanged = GetActiveFile(sciPtr) != activeFile;
                                //if file is no longer active in this scintilla we have to break!
                                if (newCts.Token.IsCancellationRequested || hasActiveFileChanged || IsNotepadShutingDown)
                                {
                                    state.Break();
                                    if (!newCts.Token.IsCancellationRequested)
                                    {
                                        //ensure that subsequent task won't run
                                        newCts.Cancel();
                                    }
                                    aSuccess = false;
                                    break;
                                }
                                aErrorDescription.AppendFormat("{0} : {2}", error.Severity, error.Line, error.Message);
                                while(aStyleOffset < aErrorDescription.Length)
                                {
                                    aStyles.Add((byte)ConvertSeverityToStyleId(error.Severity));
                                    ++aStyleOffset;
                                }
                                if (++aErrorCounter < aErrorGroup.Count())
                                {
                                    aErrorDescription.Append("\n");
                                    aStyles.Add((byte)ConvertSeverityToStyleId(error.Severity));
                                    ++aStyleOffset;
                                }
                            }
                            annotations.Add(new Tuple<int, StringBuilder, byte[]>(aErrorGroup.First().LineForScintilla, aErrorDescription, aStyles.ToArray()));
                        });
                    }, newCts.Token).ContinueWith((x) =>
                    {
                        SetAnnotations(sciPtr, annotations.OrderBy(y => y.Item1));
                        PlaceAnnotations(sciPtr);
                    }, newCts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);;
                    SetDrawingTask(sciPtr, newTask);
                }
                catch (Exception)
                {
                    Trace.WriteLine("DrawAnnotations failed.");
                    return false;
                }
            }
            else
            {
                return false;
            }
            return aSuccess;
        }

        protected override void PlaceAnnotations(IntPtr sciPtr, bool waitForTask = false)
        {
            if (!IsNotepadShutingDown)
            {
                var aIndicatorRanges = (IEnumerable<Tuple<int, StringBuilder, byte[]>>)GetAnnotations(sciPtr);
                var aVisibilityInfo  = GetVisibilityInfo(sciPtr);
                var runningTask      = GetDrawingTask(sciPtr);
                var activeFile       = GetDrawingFile(sciPtr);
                HideAnnotations(sciPtr);
                if (aIndicatorRanges != null && (!waitForTask || (runningTask == null || runningTask.IsCompleted)))
                {
                    var visibleAnnotations = from range in aIndicatorRanges
                                             where range.Item1 >= aVisibilityInfo.FirstLine && range.Item1 <= aVisibilityInfo.LastLine
                                             select range;
                    foreach(var annotation in visibleAnnotations)
                    {
                        if (IsNotepadShutingDown || (activeFile != GetActiveFile(sciPtr)))
                        {
                            return;
                        }
                        _nppHelper.AddAnnotation(annotation.Item1, annotation.Item2, sciPtr);
                        _nppHelper.SetAnnotationStyles(annotation.Item1, annotation.Item3, sciPtr);
                    }
                    _nppHelper.SetAnnotationVisible(sciPtr, Constants.Scintilla.BOXED_ANNOTATION_STYLE);
                }
            }
        }
        #endregion
    }
}
