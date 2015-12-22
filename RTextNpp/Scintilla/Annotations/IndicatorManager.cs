using RTextNppPlugin.DllExport;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
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
        private CancellationTokenSource _mainSciCts          = null;
        private CancellationTokenSource _subSciCts           = null;
        private Task _mainSciDrawingTask                     = null;
        private Task _subSciDrawningTask                     = null;
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
        private ConcurrentBag<Tuple<int, int, int>> _indicatorRangesMain = null; //!< Holds last drawn indicator ranges for main view - used to speed up deletion of ranges, rather than deleting the whole document ( time consuming ).
        private ConcurrentBag<Tuple<int, int, int>> _indicatorRangesSub  = null; //!< Holds last drawn indicator ranges for sub view - used to speed up deletion of ranges, rather than deleting the whole document ( time consuming ).
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

            plugin.ScintillaFocusChanged += OnScintillaFocusChanged;
        }

        void OnScintillaFocusChanged(IntPtr sciPtr, bool hasFocus)
        {
            var task = GetDrawingTask(sciPtr);
            var cts  = GetCts(sciPtr);
            if (!hasFocus &&( task != null && !(task.IsCanceled || task.IsCompleted)))
            {
                cts.Cancel();
                SetIndicatorsRanges(sciPtr, null);
                ResetLastAnnotatedFile(sciPtr);
            }
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorSquiggleLines);
                ProcessSettingChanged();
                if(!_areAnnotationEnabled)
                {
                    //clear cache
                    _indicatorRangesMain = null;
                    _indicatorRangesSub  = null;
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
                Plugin.Instance.ScintillaFocusChanged -= OnScintillaFocusChanged;
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
            //this even comes before buffer is activated - errors do not match with the file
            info.LastLine += 1;
            info.FirstLine = info.FirstLine > 0 ? --info.FirstLine : info.FirstLine;
            SetVisibilityInfo(info);
            if (IsWorkspaceFile(info.File) && _nppHelper.FindScintillaFromFilepath(info.File) == info.ScintillaHandle)
            {
                //update current annotations - if current file belongs in workspace and editor is focused
                PlaceIndicatorsRanges(info.ScintillaHandle);
            }
            return null;
        }
        #endregion

        #region [Helpers]

        protected override void OnBufferActivated(object source, string file)
        {
            PreProcessOnBufferActivatedEvent();
            if (!Utilities.FileUtilities.IsRTextFile(file, _settings, _nppHelper) || (ErrorList == null && IsWorkspaceFile(file)))
            {
                //remove annotations from the view which this file belongs to
                var scintilla = _nppHelper.FindScintillaFromFilepath(file);

                _nppHelper.ClearAllIndicators(scintilla, INDICATOR_INDEX);
                
                if (scintilla == _nppHelper.MainScintilla)
                {
                    _lastMainViewAnnotatedFile = string.Empty;
                }
                else
                {
                    _lastSubViewAnnotatedFile = string.Empty;
                }
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
                    _nppHelper.ClearAllIndicators(scintilla, INDICATOR_INDEX);
                }
            }
        }

        protected override void DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr)
        {
            try
            {
                //cancel any pending task
                var task = GetDrawingTask(sciPtr);
                var cts = GetCts(sciPtr);
                if (task != null && !(task.IsCanceled || task.IsCompleted))
                {
                    cts.Cancel();
                    SetIndicatorsRanges(sciPtr, null);
                    ResetLastAnnotatedFile(sciPtr);
                }
                HideAnnotations(sciPtr);
                //only grab focus - if this sciPtr has currently focus
                if (_nppHelper.GetCurrentFilePath() == FindActiveFile(sciPtr))
                {
                    _nppHelper.GrabFocus(sciPtr);
                }
                //start new task
                var newCts = new CancellationTokenSource();
                var newTask = Task.Factory.StartNew(() =>
                {
                    ConcurrentBag<Tuple<int, int, int>> indicatorRanges = GetIndicatorRanges(sciPtr);
                    if (errors != null)
                    {
                        indicatorRanges = new ConcurrentBag<Tuple<int, int, int>>();
                        var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line).AsParallel();
                        Parallel.ForEach(aErrorGroupByLines, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (aErrorGroup) =>
                        {
                            if (newCts.Token.IsCancellationRequested || !HasFocus(sciPtr))
                            {
                                indicatorRanges = null;
                                ResetLastAnnotatedFile(sciPtr);
                                return;
                            }
                            //do the heavy work here, tokenize line and try to find perfect matches in the errors - if no perfect match can be found highlight whole line
                            var aLineNumber          = aErrorGroup.First().Line - 1;
                            var aPositionAtLineStart = _nppHelper.GetLineStart(aLineNumber);
                            var aLineText            = _nppHelper.GetLine(aLineNumber, sciPtr);

                            Tokenizer tokenizer = new Tokenizer(aLineNumber, aPositionAtLineStart, aLineText);
                            bool aIsAnyMatchFound = false;
                            foreach (var t in tokenizer.Tokenize(ERROR_TOKEN_TYPES))
                            {
                                if (newCts.Token.IsCancellationRequested || !HasFocus(sciPtr))
                                {
                                    indicatorRanges = null;
                                    ResetLastAnnotatedFile(sciPtr);
                                    return;
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
                    }
                    SetIndicatorsRanges(sciPtr, indicatorRanges);
                }, newCts.Token).ContinueWith((x) =>
                {
                    PlaceIndicatorsRanges(sciPtr);
                }, newCts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                SetCts(sciPtr, newCts);
                SetDrawingTask(sciPtr, newTask);

            }
            catch (Exception)
            {
                Trace.WriteLine("Draw margins failed.");
            }
        }

        private Task GetDrawingTask(IntPtr sciPtr)
        {
            if(sciPtr == _nppHelper.MainScintilla)
            {
                return _mainSciDrawingTask;
            }
            return _subSciDrawningTask;
        }

        private CancellationTokenSource GetCts(IntPtr sciPtr)
        {
            if(sciPtr == _nppHelper.MainScintilla)
            {
                return _mainSciCts;
            }
            return _subSciCts;
        }

        private void SetDrawingTask(IntPtr sciPtr, Task task)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _mainSciDrawingTask = task;
            }
            _subSciDrawningTask = task;
        }

        private void SetCts(IntPtr sciPtr, CancellationTokenSource cts)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _mainSciCts = cts;
            }
            _subSciCts = cts;
        }

        private void SetIndicatorsRanges(IntPtr sciPtr, ConcurrentBag<Tuple<int, int, int>> bag)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _indicatorRangesMain = (bag == null) ? null : new ConcurrentBag<Tuple<int, int, int>>(bag);
                return;
            }
            _indicatorRangesSub = (bag == null) ? null : new ConcurrentBag<Tuple<int, int, int>>(bag);
        }

        private ConcurrentBag<Tuple<int, int, int>> GetIndicatorRanges(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                return _indicatorRangesMain;
            }
            return _indicatorRangesSub;
        }

        private void PlaceIndicatorsRanges(IntPtr sciPtr)
        {
            var aIndicatorRanges = GetIndicatorRanges(sciPtr);
            var aVisibilityInfo  = GetVisibilityInfo(sciPtr);
            if (aIndicatorRanges != null)
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
                    if(!HasFocus(sciPtr))
                    {
                        //critical point - avoid endless loop
                        return;
                    }
                    _nppHelper.SetCurrentIndicator(sciPtr, INDICATOR_INDEX);
                    _nppHelper.PlaceIndicator(sciPtr, ranges[i].Item1, ranges[i].Item2);
                    //ensure indicator is placed by reading it - if not stay on same indicator
                    int indicatorRangeStart = _nppHelper.IndicatorStart(sciPtr, INDICATOR_INDEX, ranges[i].Item1);
                    int indicatorRangeEnd   = _nppHelper.IndicatorEnd(sciPtr, INDICATOR_INDEX, ranges[i].Item1);

                    if(ranges[i].Item1 != indicatorRangeStart || indicatorRangeEnd != (ranges[i].Item1 + ranges[i].Item2))
                    {
                        --i;
                    }
                }
            }
        }

        private bool HasFocus(IntPtr sci)
        {
            if(_nppHelper.MainScintilla == sci)
            {
                return Plugin.Instance.HasMainSciFocus;
            }
            return Plugin.Instance.HasSecondSciFocus;
        }

        private void ResetLastAnnotatedFile(IntPtr sciPtr)
        {
            if(sciPtr == _nppHelper.MainScintilla)
            {
                _lastMainViewAnnotatedFile = string.Empty;
                return;
            }
            _lastSubViewAnnotatedFile = string.Empty;
        }

        #endregion    
    }
}
