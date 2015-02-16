using System;
using System.Drawing;
using System.Linq;
using CSScriptIntellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.Automate;
using RTextNppPlugin.Automate.Protocol;
using RTextNppPlugin.Logging;
using RTextNppPlugin.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.WpfControls;

namespace RTextNppPlugin.ViewModels
{
    internal class AutoCompletionViewModel : BindableObject, IDisposable
    {
        internal class Completion
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

            public Completion(string displayText, string insertionText, string description, AutoCompletionType glyph)
            {
                _displayText   = displayText;
                _insertionText = insertionText;
                _description   = description;
                _glyph         = glyph;
            }

            public string DisplayText { get { return _displayText; } }

            public string Description { get { return _description; } }

            public string InsertionText { get { return _insertionText; } }

            public AutoCompletionType ImageType { get { return _glyph; } }

            #endregion

            #region [Helpers]
            
            #endregion

            #region [Data Members]

            private readonly string _displayText;
            private readonly string _insertionText;
            private readonly string _description;
            private readonly AutoCompletionType _glyph;

            #endregion
        }

        #region [Interface]

        public enum MatchingType
        {
            STARTS_WITH,
            CONTAINS,
            FUZZY,
            NONE
        };

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

        public bool IsSelected { get; private set; }

        public bool IsUnique { get; private set; }

        public void OnZoomLevelChanged(int newZoomLevel)
        {
            //calculate actual zoom level , based on Scintilla zoom factors...
            
            //try 8% increments / decrements
            ZoomLevel = (1 + (0.08 * newZoomLevel));            
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
        
        public AutoCompletionViewModel()
        {
            _filteredList      = new FilteredObservableCollection<Completion>(_completionList);
            CharProcessAction  = CharProcessResult.NoAction;
            IsSelected         = false;
            IsUnique           = false;
            SelectedCompletion = null;
            _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));
            _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));
            _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));            
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public int Count
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
                    base.RaisePropertyChanged("Count");
                }
            }
        }

        public Completion SelectedCompletion {get; private set;}

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

        public FilteredObservableCollection<Completion> CompletionList
        {
            get
            {
                return _filteredList;
            }
        }

        public void AugmentAutoCompletion(ContextExtractor extractor, Point caretPoint, Tokenizer.TokenTag ? token, ref bool request)
        {
            TriggerPoint = token;
            CharProcessAction = CharProcessResult.NoAction;
            if(!token.HasValue)
            {
                CharProcessAction = CharProcessResult.ForceClose;
                return;
            }
            AutoCompleteAndReferenceRequest aRequest = new AutoCompleteAndReferenceRequest
            {
                column        = extractor.ContextColumn + 1,//compensate for backend
                command       = Constants.Commands.CONTENT_COMPLETION,
                context       = extractor.ContextList,
                type          = Constants.Commands.REQUEST,
                invocation_id = -1
            };
            _currentConnector = ConnectorManager.Instance.Connector;
            if (_currentConnector != null)
            {
                switch (_currentConnector.ConnectorState)
                {
                    case Automate.StateEngine.ProcessState.Closed:
                        _completionList.Clear();
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));
                        _currentConnector.execute<AutoCompleteAndReferenceRequest>(aRequest, ref _currentInvocationId);
                        break;
                    case Automate.StateEngine.ProcessState.Busy:
                    case Automate.StateEngine.ProcessState.Loading:
                        _completionList.Clear();
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC));
                        break;
                    case Automate.StateEngine.ProcessState.Connected:
                        if (request)
                        {
                            _completionList.Clear();
                            request = false;
                            AutoCompleteResponse aResponse = _currentConnector.execute<AutoCompleteAndReferenceRequest>(aRequest, ref _currentInvocationId, Constants.SYNCHRONOUS_COMMANDS_TIMEOUT) as AutoCompleteResponse;

                            if (aResponse == null)
                            {
                                _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_AUTO_COMPLETION_NULL_RESP, Properties.Resources.ERR_AUTO_COMPLETION_NULL_DESC));
                            }
                            else
                            {
                                //check invocation id
                                if (_currentInvocationId != aResponse.invocation_id)
                                {
                                    _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_AUTO_COMPLETION_INVOKATION, Properties.Resources.ERR_AUTO_COMPLETION_INVOKATION_DESC));
                                    Logger.Instance.Append(Logger.MessageType.Error,
                                                             _currentConnector.Workspace,
                                                             String.Format("Auto complete for file {0} failed. Wrong invocation id return from backend! Expected: {1} - Receoved: {2}",
                                                                                                                                                                 Npp.GetCurrentFile(),
                                                                                                                                                                 _currentInvocationId,
                                                                                                                                                                 aResponse.invocation_id));
                                }
                                else
                                {
                                    if (aResponse.options.Count != 0)
                                    {
                                        if (aResponse.options.Count == 1)
                                        {
                                            //auto insert
                                        }
                                        else
                                        {
                                            _completionList.AddRange(aResponse.options.Select(x => new Completion(x.display, x.insert, x.desc, Completion.AutoCompletionType.Label)).OrderBy(x => x.InsertionText));
                                        }
                                    }
                                }
                                Filter();
                            }
                        }                        
                        break;
                    default:
                        Logger.Instance.Append(Logger.MessageType.FatalError, _currentConnector.Workspace, "Undefined connector state reached. Please notify support.");
                        break;

                }
            }
            else
            {
                _completionList.Clear();
                _completionList.Add(CreateWarningCompletion(Properties.Resources.CONNECTOR_INSTANCE_NULL, Properties.Resources.CONNECTOR_INSTANCE_NULL_DESC));
            }
        }

        public void Filter()
        {
            if(TriggerPoint.HasValue && TriggerPoint.Value.Context != null)
            {
                string aHint = TriggerPoint.Value.Context;
                _filteredList.Filter(x => x.InsertionText.StartsWith(aHint, StringComparison.OrdinalIgnoreCase));
                               
                
                if(_filteredList.Count == 0)
                {
                    _filteredList.Filter(x => x.InsertionText.Contains(aHint, StringComparison.OrdinalIgnoreCase));
                    if(_filteredList.Count == 0)
                    {
                        //fuzzy matching
                        _filteredList.StopFiltering();
                    }
                }
                else
                {
                    //select the entry with minimum length
                    var bestMatch = _filteredList.Aggregate((curMin, x) => (x.InsertionText.Length < curMin.InsertionText.Length ? x : curMin));
                    SelectedIndex = _filteredList.IndexOf(bestMatch);
                    SelectedCompletion = bestMatch;
                }
            }
            else
            {
                _filteredList.StopFiltering();
                _selectedIndex = -1;
            }
            //ITextSnapshot currentSnapshot = ApplicableTo.TextBuffer.CurrentSnapshot;
            //this.mfilterBufferText = ApplicableTo.GetText(currentSnapshot).TrimEnd();
            //((FilteredObservableCollection<Completion>)Completions).StopFiltering();

            //IsAutoCompletionOptionLast = Completions.Count == 1;
            //if (this.mfilterBufferText == null || mOriginalList.Count == 1 && mOriginalList.First().InsertionText == null)
            //{
            //    _lastMatchingType = MatchingType.NONE;
            //    _lastStringWhichMatched = String.Empty;
            //    return;
            //}
            //else
            //{
            //    if (this.mfilterBufferText.Length > 0 && this.mfilterBufferText.Last() == ',')
            //    {
            //        this.mfilterBufferText = this.mfilterBufferText.Substring(0, this.mfilterBufferText.Length - 1);
            //    }
            //    //prefix match
            //    WritableCompletions.Clear();
            //    WritableCompletions.AddRange(mOriginalList.AsParallel().Where(x => x.InsertionText.StartsWith(this.mfilterBufferText, StringComparison.OrdinalIgnoreCase)));
            //    //if nothing is found.. 
            //    if (WritableCompletions.Count == 0)
            //    {
            //        WritableCompletions.AddRange(mOriginalList.AsParallel().Where(x => x.InsertionText.Contains(this.mfilterBufferText, StringComparison.OrdinalIgnoreCase)));
            //        if (WritableCompletions.Count == 0)
            //        {
            //            if (mOriginalList.Count > 5000)
            //            {
            //                return;
            //            }
            //            fuzzyMatching();
            //            if (WritableCompletions.Count == 0)
            //            {
            //                //display last matches                       
            //                this.mfilterBufferText = _lastStringWhichMatched;

            //                switch (_lastMatchingType)
            //                {
            //                    case MatchingType.STARTS_WITH:
            //                        WritableCompletions.AddRange(mOriginalList.AsParallel().Where(x => x.InsertionText.StartsWith(this.mfilterBufferText, StringComparison.OrdinalIgnoreCase)));
            //                        return;
            //                    case MatchingType.CONTAINS:
            //                        WritableCompletions.AddRange(mOriginalList.AsParallel().Where(x => x.InsertionText.Contains(this.mfilterBufferText, StringComparison.OrdinalIgnoreCase)));
            //                        return;
            //                    case MatchingType.FUZZY:
            //                        fuzzyMatching();
            //                        return;
            //                    default:
            //                        WritableCompletions.AddRange(mOriginalList);
            //                        return;
            //                }
            //            }
            //            else
            //            {
            //                _lastStringWhichMatched = mfilterBufferText;
            //                _lastMatchingType = MatchingType.FUZZY;
            //            }
            //        }
            //        else
            //        {
            //            _lastStringWhichMatched = mfilterBufferText;
            //            _lastMatchingType = MatchingType.CONTAINS;
            //        }
            //    }
            //    else
            //    {
            //        _lastStringWhichMatched = mfilterBufferText;
            //        _lastMatchingType = MatchingType.STARTS_WITH;
            //    }
            //    Trace.WriteLine(String.Format("Found : {0} completions that match prefix.", WritableCompletions.Count));
            //}
        }
        #endregion

        #region [Helpers]
        Completion CreateWarningCompletion(string warning, string desc)
        {
            return new Completion(warning, String.Empty, desc, Completion.AutoCompletionType.Warning);
        }

        private void AddCharToTriggerPoint(char c)
        {            
            if(!_triggerToken.HasValue)
            {
                CharProcessAction = CharProcessResult.ForceClose;
                return;
            }
            Tokenizer.TokenTag t = _triggerToken.Value;
            string aContext = t.Context;
            bool wasEmpty = (aContext.Length == 0);
            if (wasEmpty && Char.IsWhiteSpace(c))
            {
                CharProcessAction = CharProcessResult.MoveToRight;
                return;
            }
            if(wasEmpty && c == Constants.BACKSPACE)
            {
                CharProcessAction = CharProcessResult.ForceClose;
                return;
            }

            int aCurrentPosition = CSScriptIntellisense.Npp.GetCaretPosition();
            if (aCurrentPosition >= 0)
            {
                int aLineNumber = CSScriptIntellisense.Npp.GetLineNumber();
                //if auto completion is inside comment, notation, name, string jusr return
                AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(aLineNumber, aCurrentPosition, Npp.GetColumn());
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

        #endregion

        #region [Data Members]
        private readonly BulkObservableCollection<Completion> _completionList = new BulkObservableCollection<Completion>();
        private readonly FilteredObservableCollection<Completion> _filteredList = null;
        private Tokenizer.TokenTag? _triggerToken = null;
        private string _lastStringWhichMatched = String.Empty;
        private Connector _currentConnector = null;
        private MatchingType _lastMatchingType = MatchingType.NONE;
        private int _currentInvocationId = -1;
        private int _count = 0;
        private double _zoomLevel = 1.0;
        private int _selectedIndex = 0;
        #endregion
    }
}
