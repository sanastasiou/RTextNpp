using RTextNppPlugin.DllExport;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace RTextNppPlugin.Scintilla.Annotations
{
    class IndicatorManager : ErrorBase, IError
    {
        #region [Data Members]
        private const Settings.RTextNppSettings SETTING      = Settings.RTextNppSettings.EnableErrorSquiggleLines;
        private const int INDICATOR_INDEX                    = 8;
        private readonly RTextTokenTypes[] ERROR_TOKEN_TYPES =  
        { 
            RTextTokenTypes.Boolean,
            RTextTokenTypes.Comma,
            RTextTokenTypes.Command,
            RTextTokenTypes.Float,
            RTextTokenTypes.Identifier,
            RTextTokenTypes.Integer,
            RTextTokenTypes.Label,
            RTextTokenTypes.QuotedString,
            RTextTokenTypes.Reference,
            RTextTokenTypes.Template
        };
        #endregion

        #region [Interface]
        internal IndicatorManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot, ILineVisibilityObserver lineVisibilityObserver, double updateDelay = Constants.Scintilla.ANNOTATIONS_UPDATE_DELAY) :
            base(settings, nppHelper, plugin, workspaceRoot, lineVisibilityObserver)
        {
            _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorSquiggleLines);

            HideAnnotations(_nppHelper.MainScintilla);
            HideAnnotations(_nppHelper.SecondaryScintilla);

            _nppHelper.SetIndicatorStyle(_nppHelper.MainScintilla, INDICATOR_INDEX, SciMsg.INDIC_SQUIGGLE, Color.Red);
            _nppHelper.SetIndicatorStyle(_nppHelper.SecondaryScintilla, INDICATOR_INDEX, SciMsg.INDIC_SQUIGGLE, Color.Red);
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                bool aNewSettingValue = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorSquiggleLines);
                if(aNewSettingValue != _areAnnotationEnabled)
                {
                    ProcessSettingChanged(aNewSettingValue);
                }
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
            }
            base.Dispose(disposing);

        }

        protected override Constants.StyleId ConvertSeverityToStyleId(ErrorItemViewModel.SeverityType severity)
        {
            //not needed here
            return Constants.StyleId.DEFAULT;
        }

        #region [Properties]

        /**
         * \brief   Gets or sets a list of errors.
         *
         * \return  A List of errors.
         * \remarks Override this property. Error matching should not run again if errors aren't updated.
         */

        new public IList<ErrorListViewModel> ErrorList
        {
            get
            {
                return _currentErrors;
            }
            set
            {
                if (value != null)
                {
                    _currentErrors       = new List<ErrorListViewModel>(value);
                    _indicatorRangesMain = null;
                    _indicatorRangesSub  = null;
                }
            }
        }
        #endregion


        #endregion

        #region [Event Handlers]
        /*
         * This event always occurs after SCN_UPDATEUI which is considered the best place to update the editor annotations.
         */
        protected override object OnVisibilityInfoUpdated(VisibilityInfo info)
        {
            if (info != GetVisibilityInfo(info.ScintillaHandle))
            {
                //this event comes before buffer is activated - errors do not match with the file
                base.OnVisibilityInfoUpdated(info);
                if (IsWorkspaceFile(info.File))
                {
                    //update current annotations
                    PlaceAnnotations(info.ScintillaHandle, true);
                }
            }
            return null;
        }

        protected override void OnBufferActivated(object source, string file, View view)
        {
            if (!IsNotepadShutingDown)
            {
                PreProcessOnBufferActivatedEvent(file, view);
                if (!Utilities.FileUtilities.IsRTextFile(file, _settings, _nppHelper) || ((ErrorList == null || !_areAnnotationEnabled) && IsWorkspaceFile(file)))
                {
                    //remove annotations from the view which this file belongs to
                    var scintilla = _nppHelper.ScintillaFromView(view);

                    _nppHelper.ClearAllIndicators(scintilla, INDICATOR_INDEX);

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
        }
        #endregion

        #region [Helpers]

        protected override void HideAnnotations(IntPtr scintilla)
        {
            var aActiveFile = _nppHelper.GetActiveFile(scintilla);
            if (!string.IsNullOrEmpty(aActiveFile) && Utilities.FileUtilities.IsRTextFile(aActiveFile, _settings, _nppHelper))
            {
                if (IsWorkspaceFile(aActiveFile))
                {
                    _nppHelper.ClearAllIndicators(scintilla, INDICATOR_INDEX);
                }
            }
        }

        protected override bool DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr)
        {
            bool aSuccess = true;
            if (!IsNotepadShutingDown)
            {
                try
                {
                    //cancel any pending task
                    var task = GetDrawingTask(sciPtr);
                    var cts  = GetCts(sciPtr);
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
                    var newCts                                          = new CancellationTokenSource();
                    ConcurrentBag<Tuple<int, int, int>> indicatorRanges = new ConcurrentBag<Tuple<int, int, int>>();
                    if (errors == null || errors.ErrorList == null || errors.ErrorList.Count == 0)
                    {
                        SetAnnotations(sciPtr, indicatorRanges);
                        return false;
                    }
                    string activeFile = errors.FilePath.Replace("/", "\\");
                    SetDrawingFile(sciPtr, activeFile);
                    SetCts(sciPtr, newCts);
                    var newTask = Task.Factory.StartNew(() =>
                    {
                        var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line);
                        Parallel.ForEach( aErrorGroupByLines,
                                          new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : Environment.ProcessorCount },
                                          (IGrouping<int, ErrorItemViewModel> aErrorGroup, ParallelLoopState state) =>
                        {
                            //do the heavy work here, tokenize line and try to find perfect matches in the errors - if no perfect match can be found highlight whole line
                            var aLineNumber          = aErrorGroup.First().Line - 1;
                            var aPositionAtLineStart = _nppHelper.GetLineStart(aLineNumber, sciPtr);
                            var aLineText            = _nppHelper.GetLine(aLineNumber, sciPtr);
                            Tokenizer tokenizer      = new Tokenizer(aLineNumber, aPositionAtLineStart, aLineText);
                            bool aIsAnyMatchFound    = false;
                            foreach (var t in tokenizer.Tokenize(ERROR_TOKEN_TYPES))
                            {
                                bool hasActiveFileChanged = GetActiveFile(sciPtr) != activeFile;
                                //if file is no longer active in this scintilla we have to break!
                                if (newCts.Token.IsCancellationRequested || hasActiveFileChanged || IsNotepadShutingDown)
                                {
                                    SetActiveFile(sciPtr, string.Empty);
                                    state.Break();
                                    if (!newCts.Token.IsCancellationRequested)
                                    {
                                        //ensure that subsequent task won't run
                                        newCts.Cancel();
                                    }
                                    aSuccess = false;
                                    break;
                                }
                                //if t is contained exactly in any of the errors, mark it as indicator
                                var matches = from m in aErrorGroup
                                              where m.Message.Contains(t.Context)
                                              select m;
                                if (matches.Count() > 0)
                                {
                                    indicatorRanges.Add(new Tuple<int, int, int>(t.BufferPosition, t.Context.Length, aLineNumber));
                                    aIsAnyMatchFound = true;
                                }
                            }
                            if (!aIsAnyMatchFound)
                            {
                                //highlight whole line
                                indicatorRanges.Add(new Tuple<int, int, int>(aPositionAtLineStart, aLineText.Length, aLineNumber));
                            }
                        });
                        
                    }, newCts.Token).ContinueWith((x) =>
                    {
                        SetAnnotations(sciPtr, indicatorRanges);
                        PlaceAnnotations(sciPtr);
                    }, newCts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                    SetDrawingTask(sciPtr, newTask);

                }
                catch (Exception)
                {
                    Trace.WriteLine("Draw indicators failed.");
                }
            }
            return aSuccess;
        }

        protected override void PlaceAnnotations(IntPtr sciPtr, bool waitForTask = false)
        {
            if (!IsNotepadShutingDown)
            {
                var aIndicatorRanges = (IEnumerable<Tuple<int, int, int>>)GetAnnotations(sciPtr);
                var aVisibilityInfo  = GetVisibilityInfo(sciPtr);
                var runningTask      = GetDrawingTask(sciPtr);
                var activeFile       = GetDrawingFile(sciPtr);

                if (aIndicatorRanges != null && (!waitForTask || (runningTask == null || runningTask.IsCompleted)))
                {
                    _nppHelper.SetIndicatorStyle(sciPtr, INDICATOR_INDEX, SciMsg.INDIC_SQUIGGLE, Color.Red);
                    _nppHelper.SetCurrentIndicator(sciPtr, INDICATOR_INDEX);
                    //get only ranges which belong to visible lines
                    var visibleRanges = from range in aIndicatorRanges
                                        where range.Item3 >= aVisibilityInfo.FirstLine && range.Item3 <= aVisibilityInfo.LastLine
                                        select range;

                    var ranges = visibleRanges.OrderBy(x => x.Item3).ToArray();
                    for (int i = 0; i < ranges.Count(); ++i)
                    {
                        if (IsNotepadShutingDown || (activeFile != GetActiveFile(sciPtr)))
                        {
                            //critical point - avoid endless loop
                            return;
                        }
                        _nppHelper.SetCurrentIndicator(sciPtr, INDICATOR_INDEX);
                        _nppHelper.PlaceIndicator(sciPtr, ranges[i].Item1, ranges[i].Item2);
                        Trace.WriteLine(String.Format("Placing indicator : line {0} - length : {1}", ranges[i].Item3, ranges[i].Item2));
                        //ensure indicator is placed by reading it - if not stay on same indicator
                        int indicatorRangeStart = _nppHelper.IndicatorStart(sciPtr, INDICATOR_INDEX, ranges[i].Item1);
                        int indicatorRangeEnd   = _nppHelper.IndicatorEnd(sciPtr, INDICATOR_INDEX, ranges[i].Item1);

                        if (ranges[i].Item1 != indicatorRangeStart || indicatorRangeEnd != (ranges[i].Item1 + ranges[i].Item2))
                        {
                            --i;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
