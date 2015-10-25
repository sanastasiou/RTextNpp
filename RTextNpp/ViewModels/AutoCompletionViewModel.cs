using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using FuzzyString;
using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.Logging;
using RTextNppPlugin.RText;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.RText.Protocol;
using RTextNppPlugin.RText.StateEngine;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.WpfControls;
namespace RTextNppPlugin.ViewModels
{
    internal class AutoCompletionViewModel : BindableObject
    {
        internal class Completion : BindableObject
        {
            #region [Interface]
            public enum AutoCompletionType
            {
                Label,
                Value,
                Reference,
                Event,
                String,
                Other,
                Warning
            }
            public Completion(string displayText, string insertionText, string description, AutoCompletionType glyph, bool isFuzzy = false)
            {
                _displayText   = displayText;
                _insertionText = insertionText;
                _description   = description;
                _glyph         = glyph;
                _isFuzzy       = isFuzzy;
            }
            public Completion(Completion completion, bool isFuzzy)
            {
                _displayText   = completion.DisplayText;
                _insertionText = completion.InsertionText;
                _description   = completion.Description;
                _glyph         = completion.ImageType;
                _isFuzzy       = isFuzzy;
            }
            public Completion(Completion completion)
            {
                _displayText   = completion.DisplayText;
                _insertionText = completion.InsertionText;
                _description   = completion.Description;
                _glyph         = completion.ImageType;
                IsFuzzy        = IsSelected = false;
            }
            public string DisplayText { get { return _displayText; } }
            public string Description { get { return _description; } }
            public string InsertionText { get { return _insertionText; } }
            public AutoCompletionType ImageType { get { return _glyph; } }
            public bool IsFuzzy
            {
                get
                {
                    return _isFuzzy;
                }
                set
                {
                    if( value != _isFuzzy)
                    {
                        _isFuzzy = value;
                        base.RaisePropertyChanged("IsFuzzy");
                    }
                }
            }
            public bool IsSelected
            {
                get
                {
                    return _isSelected;
                }
                set
                {
                    if (value != _isSelected)
                    {
                        _isSelected = value;
                        base.RaisePropertyChanged("IsSelected");
                    }
                }
            }
            #endregion
            #region [Helpers]
            #endregion
            #region [Data Members]
            private readonly string _displayText;
            private readonly string _insertionText;
            private readonly string _description;
            private readonly AutoCompletionType _glyph;
            private bool _isFuzzy;
            private bool _isSelected;
            #endregion
        }
        #region [Interface]
        public enum CharProcessResult
        {
            ForceClose,
            ForceCommit,
            NoAction,
            MoveToRight
        }
        /**
         * Executes the key pressed action.
         *
         * \remark  Enter, Tab, Esc, Cancel and other character types are not handled here.
         *          This function only adjusts the trigger and insertion points by analyzing
         *          Backspace, Space and visible characters.
         *
         * \param   key The key.
         */
        public void OnKeyPressed(char c)
        {
            CharProcessAction = CharProcessResult.NoAction;
            AddCharToTriggerPoint(c);
        }
        public CharProcessResult CharProcessAction { get; private set; }
        public Tokenizer.TokenTag? TriggerPoint
        {
            get
            {
                return _triggerToken;
            }
            private set
            {
                _triggerToken = value;
            }
        }
        public void OnZoomLevelChanged(double newZoomLevel)
        {
            //calculate actual zoom level , based on Scintilla zoom factors...
            //try 8% increments / decrements
            ZoomLevel = (1.0 + (Constants.ZOOM_FACTOR * newZoomLevel));
        }
        public double ZoomLevel
        {
            get
            {
                return _zoomLevel;
            }
            set
            {
                if (value != _zoomLevel)
                {
                    _zoomLevel = value;
                    base.RaisePropertyChanged("ZoomLevel");
                }
            }
        }
        public AutoCompletionViewModel(ConnectorManager cmanager)
        {
            _cManager          = cmanager;
            _filteredList      = new FilteredObservableCollection<Completion>(_completionList);
            FilteredCount      = 0;
            CharProcessAction  = CharProcessResult.NoAction;
            SelectedCompletion = null;
            _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));
            Pending = false;
        }
        public void OnAutoCompletionWindowCollapsing()
        {
            _completionList.Clear();
            _filteredList.StopFiltering();
            if (SelectedCompletion != null)
            {
                SelectedCompletion.IsFuzzy    = false;
                SelectedCompletion.IsSelected = false;
                SelectedCompletion            = null;
            }
            if (_connector != null)
            {
                _connector.CancelCommand();
            }
        }
        public bool Pending { get; private set; }
        public int FilteredCount
        {
            get
            {
                return _count;
            }
            set
            {
                if(value != _count)
                {
                    _count = value;
                    base.RaisePropertyChanged("FilteredCount");
                }
            }
        }
        public void SelectPosition(int newPosition)
        {
            //fix for initial not selected item, when filtering ignore index change events
            if(newPosition == - 1 || _isFiltering)
            {
                return;
            }
            if(newPosition < 0 || newPosition > _filteredList.Count)
            {
                throw new ArgumentException(String.Format("newPosition is out of range : {0}", newPosition));
            }
            if (SelectedCompletion != null)
            {
                SelectedCompletion.IsFuzzy = SelectedCompletion.IsSelected = false;
            }
            SelectedCompletion         = _filteredList[newPosition];
            SelectedCompletion.IsFuzzy = SelectedCompletion.IsSelected = true;
        }
        public Completion SelectedCompletion
        {
            get
            {
                return _selectedCompletion;
            }
            private set
            {
                if(value != _selectedCompletion)
                {
                    _selectedCompletion = value;
                    base.RaisePropertyChanged("SelectedCompletion");
                }
            }
        }
        public BulkObservableCollection<Completion> UnderlyingList
        {
            get
            {
                return _completionList;
            }
        }
        public FilteredObservableCollection<Completion> CompletionList
        {
            get
            {
                return _filteredList;
            }
        }
        async public Task AugmentAutoCompletion(ContextExtractor extractor, Point caretPoint, AutoCompletionTokenizer tokenizer)
        {
            Pending = true;
            CharProcessAction = CharProcessResult.NoAction;
            _completionList.Clear();
            if (!tokenizer.TriggerToken.HasValue)
            {
                CharProcessAction = CharProcessResult.ForceClose;
                Pending = false;
                return;
            }
            TriggerPoint = tokenizer.TriggerToken;
            bool areContextEquals = false;
            //get all tokens before the trigger token - if all previous tokens and all context lines match do not request new auto completion options
            if (_cachedOptions != null && _cachedContext != null && !_isWarningCompletionActive)
            {
                if (_cachedContext.Count() == 1 && extractor.ContextList.Count() == 1)
                {
                    areContextEquals = (_equalityComparer.AreTokenStreamsEqual(tokenizer.LineTokens, Npp.Instance.GetCaretPosition(), Npp.Instance.GetCurrentFilePath()));
                }
                else
                {
                    //if context is identical and tokens are also identical do not trigger auto completion request
                    areContextEquals = (_cachedContext.Take(_cachedContext.Count() - 1).SequenceEqual(extractor.ContextList.Take(extractor.ContextList.Count() - 1)) &&
                                        _equalityComparer.AreTokenStreamsEqual(tokenizer.LineTokens, Npp.Instance.GetCaretPosition(), Npp.Instance.GetCurrentFilePath()));
                }
            }
            else
            {
                //prime comparer
                _equalityComparer.AreTokenStreamsEqual(tokenizer.LineTokens, Npp.Instance.GetCaretPosition(), Npp.Instance.GetCurrentFilePath());
            }
            if (areContextEquals)
            {
                _completionList.AddRange(_cachedOptions);
                Pending = false;
                Filter();
                return;
            }
            //store cache
            _cachedContext = extractor.ContextList;
            AutoCompleteAndReferenceRequest aRequest = new AutoCompleteAndReferenceRequest
            {
                column        = extractor.ContextColumn,
                command       = Constants.Commands.CONTENT_COMPLETION,
                context       = extractor.ContextList,
                invocation_id = -1
            };
            _connector = _cManager.Connector;
            if (_connector != null)
            {
                switch (_connector.CurrentState.State)
                {
                    case ConnectorStates.Disconnected:
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));
                        _isWarningCompletionActive = true;
                        await _connector.ExecuteAsync<AutoCompleteAndReferenceRequest>(aRequest, Constants.SYNCHRONOUS_COMMANDS_TIMEOUT, Command.Execute);
                        break;
                    case ConnectorStates.Busy:
                    case ConnectorStates.Loading:
                    case ConnectorStates.Connecting:
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC));
                        _isWarningCompletionActive = true;
                        break;
                    case ConnectorStates.Idle:
                        Pending = true;
                        AutoCompleteResponse aResponse = await _connector.ExecuteAsync<AutoCompleteAndReferenceRequest>(aRequest, Constants.SYNCHRONOUS_COMMANDS_TIMEOUT, Command.Execute) as AutoCompleteResponse;
                        if (aResponse == null)
                        {
                            if (_connector.IsCommandCancelled)
                            {
                                CharProcessAction = CharProcessResult.ForceClose;
                            }
                            else
                            {
                                _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_AUTO_COMPLETION_NULL_RESP, Properties.Resources.ERR_AUTO_COMPLETION_NULL_DESC));
                                _isWarningCompletionActive = true;
                            }
                        }
                        else
                        {
                            if(_isWarningCompletionActive)
                            {
                                _completionList.Clear();
                                _isWarningCompletionActive = false;
                            }
                            //add pics
                            var labeledList = aResponse.options.AsParallel().Select(x => new Completion(
                                x.display,
                                x.insert,
                                string.IsNullOrEmpty(x.desc) ? "No description available." : x.desc,
                                DetermineCompletionImage(x.display)));
                            if (labeledList.Count() != 0)
                            {
                                if (labeledList.Count() == 1)
                                {
                                    //auto insert
                                    SelectedCompletion = labeledList.First();
                                    SelectedCompletion.IsSelected = true;
                                    _completionList.Add(labeledList.First());
                                    CharProcessAction = CharProcessResult.ForceCommit;
                                }
                                else
                                {
                                    _completionList.AddRange(labeledList.OrderBy(x => x.InsertionText));
                                    Pending = false;
                                    Filter();
                                }
                                _cachedOptions = new List<Completion>(_completionList);
                            }
                            else
                            {
                                _cachedOptions = null;
                                CharProcessAction = CharProcessResult.ForceClose;
                            }
                            _isWarningCompletionActive = false;
                        }
                        break;
                    default:
                        Logger.Instance.Append(Logger.MessageType.FatalError, _connector.Workspace, "Undefined connector state reached. Please notify support.");
                        _isWarningCompletionActive = true;
                        break;
                }
            }
            else
            {
                _isWarningCompletionActive = true;
                _completionList.Add(CreateWarningCompletion(Properties.Resources.CONNECTOR_INSTANCE_NULL, Properties.Resources.CONNECTOR_INSTANCE_NULL_DESC));
            }
        }
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            set
            {
                if(value != _selectedIndex)
                {
                    _selectedIndex = value;
                    base.RaisePropertyChanged("SelectedIndex");
                }
            }
        }
        public void Filter()
        {
            _isFiltering = true;
            string fallBackHint  = _previousHint;
            if (SelectedCompletion != null)
            {
                SelectedCompletion.IsSelected = SelectedCompletion.IsFuzzy = false;
            }
            if(TriggerPoint.HasValue && !String.IsNullOrWhiteSpace(TriggerPoint.Value.Context))
            {
                _previousHint       = TriggerPoint.Value.Context;
                int aprefixCount    = 0;
                int aContainedCount = 0;
                int aFuzzyCount     = 0;
                _filteredList.Filter((x) =>
                {
                    if (x.InsertionText.StartsWith(_previousHint, StringComparison.OrdinalIgnoreCase))
                    {
                        ++aprefixCount;
                        return true;
                    }
                    else if (x.InsertionText.Contains(_previousHint, StringComparison.OrdinalIgnoreCase))
                    {
                        ++aContainedCount;
                        return true;
                    }
                    else if ((_completionList.Count() < 2000) && x.InsertionText.ApproximatelyEquals(_previousHint, FuzzyStringComparisonTolerance.Strong, APPROXIMATION_CRITERIA))
                    {
                        ++aFuzzyCount;
                        return true;
                    }
                    return false;
                });
                if ((FilteredCount = _filteredList.Count) == 0)
                {
                    _filteredList.StopFiltering();
                    if (SelectedCompletion != null)
                    {
                        SelectedIndex = _filteredList.IndexOf(SelectedCompletion);
                    }
                    if(SelectedIndex == -1)
                    {
                        if (_filteredList.Count > 0)
                        {
                            SelectedCompletion = _filteredList.First();
                            SelectedIndex      = 0;
                        }
                        else
                        {
                            if(SelectedCompletion != null)
                            {
                                SelectedCompletion.IsSelected = SelectedCompletion.IsFuzzy = false;
                            }
                            SelectedCompletion = null;
                        }
                    }
                    if (SelectedCompletion != null)
                    {
                        SelectedCompletion.IsFuzzy    = true;
                        SelectedCompletion.IsSelected = false;
                    }
                    FilteredCount = _completionList.Count;
                }
                else
                {
                    //select with priority to prefix
                    if(aprefixCount > 0)
                    {
                        SelectedCompletion = _filteredList.Where(x => x.InsertionText.StartsWith(_previousHint, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.InsertionText.Length).First();
                    }
                    else if(aContainedCount > 0)
                    {
                        SelectedCompletion = _filteredList.Where(x => x.InsertionText.Contains(_previousHint, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.InsertionText.Length).First();
                    }
                    else
                    {
                        SelectedCompletion = _filteredList.OrderBy(x => x.InsertionText.Length).First();
                    }
                    SelectedIndex = _filteredList.IndexOf(SelectedCompletion);
                    SelectedCompletion.IsSelected = SelectedCompletion.IsFuzzy = true;
                }
            }
            else
            {
                _filteredList.StopFiltering();
                if (_filteredList.Count > 0)
                {
                    SelectedCompletion = _filteredList.First();
                }
                SelectedCompletion.IsFuzzy    = true;
                SelectedCompletion.IsSelected = false;
                FilteredCount                 = _completionList.Count;
                SelectedIndex                 = -1;
            }
            _isFiltering = false;
        }
        #endregion
        #region [Helpers]
        Completion CreateWarningCompletion(string warning, string desc)
        {
            return new Completion(warning, String.Empty, desc, Completion.AutoCompletionType.Warning);
        }
        private void AddCharToTriggerPoint(char c)
        {
            int aCurrentPosition = Npp.Instance.GetCaretPosition();
            int aLineNumber      = Npp.Instance.GetLineNumber();
            if(!_triggerToken.HasValue)
            {
                CharProcessAction = CharProcessResult.ForceClose;
                return;
            }
            Tokenizer.TokenTag t = TriggerPoint.Value;
            string aContext = t.Context;
            bool wasEmpty = (aContext.Length == 0 || t.Type == RTextTokenTypes.Space);
            if (char.IsWhiteSpace(c) && (wasEmpty || t.Type == RTextTokenTypes.Comma ))
            {
                CharProcessAction = CharProcessResult.MoveToRight;
                AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(aLineNumber, aCurrentPosition, Npp.Instance);
                TriggerPoint = aTokenizer.TriggerToken;
                return;
            }
            if(wasEmpty && c == Constants.BACKSPACE)
            {
                CharProcessAction = CharProcessResult.ForceClose;
                return;
            }
            if (aCurrentPosition >= 0)
            {
                AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(aLineNumber, aCurrentPosition, Npp.Instance);
                TriggerPoint = aTokenizer.TriggerToken;
                if (!TriggerPoint.HasValue)
                {
                    CharProcessAction = CharProcessResult.ForceClose;
                }
            }
            else
            {
                CharProcessAction = CharProcessResult.ForceClose;
            }
        }
        private Completion.AutoCompletionType DetermineCompletionImage(string displayText)
        {
            if(displayText.Contains('/'))
            {
                return Completion.AutoCompletionType.Reference;
            }
            if(displayText.Contains("enum", StringComparison.InvariantCultureIgnoreCase))
            {
                return Completion.AutoCompletionType.Other;
            }
            if(displayText.Contains("string", StringComparison.InvariantCultureIgnoreCase))
            {
                return Completion.AutoCompletionType.Value;
            }
            if(displayText.Contains("event", StringComparison.InvariantCultureIgnoreCase))
            {
                return Completion.AutoCompletionType.Event;
            }
            if( displayText.Contains("literal", StringComparison.InvariantCultureIgnoreCase) ||
                displayText.Contains("float", StringComparison.InvariantCultureIgnoreCase)   ||
                displayText.Contains("integer", StringComparison.InvariantCultureIgnoreCase))
            {
                return Completion.AutoCompletionType.Value;
            }
            return Completion.AutoCompletionType.Label;
        }
        #endregion
        #region [Data Members]
        private readonly BulkObservableCollection<Completion> _completionList      = new BulkObservableCollection<Completion>(); //!< Underlying completion list.
        private readonly FilteredObservableCollection<Completion> _filteredList    = null;                                       //!< UI completion list based on underlying list.
        private readonly ConnectorManager _cManager                                = null;                                       //!< Connector manager.
        private Tokenizer.TokenTag? _triggerToken                                  = null;                                       //!< Current completion list trigger token.
        private int _count                                                         = 0;                                          //!< Options count.
        private double _zoomLevel                                                  = 1.0;                                        //!< Auto completion window zoom level.
        private Completion _selectedCompletion                                     = null;                                       //!< Currently selected option.
        private string _previousHint                                               = String.Empty;                               //!< Last string which matched an auto completion option.
        private List<Completion> _cachedOptions                                    = null;                                       //!< Last set of options.
        private bool _isWarningCompletionActive                                    = false;                                      //!< Indicates if a warning option was active.
        private int _selectedIndex                                                 = 0;                                          //!< Indicates the selected index of the filtered option list.
        private bool _isFiltering                                                  = false;                                      //!< Indicates if filtering function is currently active.
        private IEnumerable<string> _cachedContext                                 = null;                                       //!< Holds the last context used for an auto completion request.
        private Connector _connector                                               = null;                                       //!< Connector for this auto completion session.
        private TokenEqualityComparer _equalityComparer                            = new TokenEqualityComparer();                //!< Compares two tokens list for similiary.
        private readonly FuzzyStringComparisonOptions[] APPROXIMATION_CRITERIA     = new FuzzyStringComparisonOptions[]          //!< Used for fuzzy matching of auto completion options.
        {
            FuzzyStringComparisonOptions.UseHammingDistance,
            FuzzyStringComparisonOptions.UseJaccardDistance,
            FuzzyStringComparisonOptions.UseOverlapCoefficient,
            FuzzyStringComparisonOptions.UseSorensenDiceDistance,
            FuzzyStringComparisonOptions.UseLongestCommonSubstring
        };
        #endregion
    }
}