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
            base(settings, nppHelper, plugin, workspaceRoot, lineVisibilityObserver, updateDelay)
        {
            _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorSquiggleLines);

            HideAnnotations(_nppHelper.MainScintilla);
            HideAnnotations(_nppHelper.SecondaryScintilla);

            _nppHelper.SetIndicatorStyle(_nppHelper.MainScintilla, INDICATOR_INDEX, SciMsg.INDIC_SQUIGGLE, Color.Red);
            _nppHelper.SetIndicatorStyle(_nppHelper.SecondaryScintilla, INDICATOR_INDEX, SciMsg.INDIC_SQUIGGLE, Color.Red);

            plugin.ScintillaUiPainted += OnScintillaUiPainted;
        }

        void OnScintillaUiPainted()
        {
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorSquiggleLines);
                ProcessSettingChanged();
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
                Plugin.Instance.ScintillaUiPainted -= OnScintillaUiPainted;
            }
            base.Dispose(disposing);

        }

        protected override Constants.StyleId ConvertSeverityToStyleId(ErrorItemViewModel.SeverityType severity)
        {
            //not needed here
            return Constants.StyleId.DEFAULT;
        }

        /*
         * This event always occurs after SCN_UPDATEUI which is considered the best place to update the editor annotations.
         */
        protected override void OnVisibilityInfoUpdated(VisibilityInfo info)
        {
            _currentVisibilityInfo = info;
            //update current annotations
            PlaceIndicatorsRanges(info.ScintillaHandle);
        }

        #endregion

        #region [Event Handlers]

        #endregion

        #region [Helpers]

        protected override object OnBufferActivated(string file)
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
            return null;
        }

        protected override void HideAnnotations(IntPtr scintilla)
        {
            //takes too long for large files -> makes it async use same task as drawing

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
                var cts  = GetCts(sciPtr);
                //takes too long..
                HideAnnotations(sciPtr);
                if(task != null && !task.IsCompleted)
                {
                    cts.Cancel();
                    task.Wait();
                }
                //start new task
                var newCts = new CancellationTokenSource();
                var newTask = Task<IEnumerable<Tuple<int,int,int>>>.Factory.StartNew( () =>
                {
                    ConcurrentBag<Tuple<int, int, int>> indicatorRanges = new ConcurrentBag<Tuple<int, int, int>>();
                    if (errors != null)
                    {
                        var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line).AsParallel();
                        Parallel.ForEach(aErrorGroupByLines, (aErrorGroup) =>
                        {
                            if(newCts.Token.IsCancellationRequested)
                            {
                                return;
                            }
                            //do the heavy work here, tokenize line and try to find perfect matches in the errors - if no perfect match can be found highlight whole line
                            var line              = aErrorGroup.First().Line - 1;
                            Tokenizer tokenizer   = new Tokenizer(line, _nppHelper.GetLine(line, sciPtr), _nppHelper);
                            bool aIsAnyMatchFound = false;
                            foreach (var t in tokenizer.Tokenize(ERROR_TOKEN_TYPES))
                            {
                                //if t is contained exactly in any of the errors, mark it as indicator
                                var matches = from m in aErrorGroup
                                              where m.Message.Contains(t.Context)
                                              select m;
                                if(matches.Count() > 0)
                                {
                                    indicatorRanges.Add(new Tuple<int, int, int>(t.BufferPosition, t.Context.Length, line));
                                    aIsAnyMatchFound = true;
                                }
                            }
                            if(!aIsAnyMatchFound)
                            {
                                //highlight whole line
                            }
                        });
                    }
                    SetIndicatorsRanges(sciPtr, ref indicatorRanges);
                    return indicatorRanges;
                }, newCts.Token);

                newTask.ContinueWith((x) => {
                    PlaceIndicatorsRanges(sciPtr);
                }, TaskContinuationOptions.OnlyOnRanToCompletion);

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

        private void SetIndicatorsRanges(IntPtr sciPtr, ref ConcurrentBag<Tuple<int, int, int>> bag)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                _indicatorRangesMain = new ConcurrentBag<Tuple<int, int, int>>(bag);
                return;
            }
            _indicatorRangesSub = new ConcurrentBag<Tuple<int, int, int>>(bag);
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
            _nppHelper.SetIndicatorStyle(sciPtr, INDICATOR_INDEX, SciMsg.INDIC_SQUIGGLE, Color.Red);
            _nppHelper.SetCurrentIndicator(sciPtr, INDICATOR_INDEX);
            //get only ranges which belong to visible lines
            var visibleRanges = from range in GetIndicatorRanges(sciPtr)
                                where range.Item3 >= _currentVisibilityInfo.FirstLine && range.Item3 <= _currentVisibilityInfo.LastLine
                                select range;

            foreach (var range in visibleRanges)
            {
                _nppHelper.PlaceIndicator(sciPtr, INDICATOR_INDEX, range.Item1, range.Item2);
            }
        }

        //
        //private void ClearIndicators(IntPtr sciPtr)
        //{
        //    var ranges = GetIndicatorRanges(sciPtr);
        //
        //    if (ranges != null)
        //    {
        //        foreach (var range in ranges)
        //        {
        //            _nppHelper.ClearIndicator(sciPtr, INDICATOR_INDEX, range.Item1, range.Item2);
        //        }
        //    }
        //}

        #endregion    
    }
}
