using CSScriptIntellisense;
using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.Automate;
using RTextNppPlugin.Automate.Protocol;
using RTextNppPlugin.Logging;
using RTextNppPlugin.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.WpfControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
                _displayText = completion.DisplayText;
                _insertionText = completion.InsertionText;
                _description = completion.Description;
                _glyph = completion.ImageType;
                IsFuzzy = IsSelected = false;
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
            SelectedCompletion = null;
            _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));       
        }

        public void OnAutoCompletionWindowCollapsing()
        {
            _cachedOptions = new List<Completion>(_completionList);
            _completionList.Clear();
            _filteredList.StopFiltering();
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

        public void SelectPosition(int newPosition)
        {
            //fix for initial not selected item
            if(newPosition == - 1)
            {
                return;
            }
            if(newPosition < 0 || newPosition > _filteredList.Count)
            {
                throw new ArgumentException(String.Format("newPosition is out of range : {0}", newPosition));
            }
            var previousSelection = SelectedCompletion;
            if (previousSelection != null)
            {
                previousSelection.IsFuzzy = previousSelection.IsSelected = false;
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

        public FilteredObservableCollection<Completion> CompletionList
        {
            get
            {
                return _filteredList;
            }
        }

        public void AugmentAutoCompletion(ContextExtractor extractor, Point caretPoint, Tokenizer.TokenTag ? token, ref bool request)
        {
            _completionList.Clear();
            if((TriggerPoint.HasValue && token.HasValue && TriggerPoint.Value == token.Value && _cachedOptions != null) && !_isWarningCompletionActive)
            {
                TriggerPoint = token;
                _completionList.AddRange(_cachedOptions);
                Filter();
                return;
            }
            
            TriggerPoint      = token;
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
                        
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));
                        _currentConnector.execute<AutoCompleteAndReferenceRequest>(aRequest, ref _currentInvocationId);
                        _isWarningCompletionActive = true;
                        break;
                    case Automate.StateEngine.ProcessState.Busy:
                    case Automate.StateEngine.ProcessState.Loading:
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC));
                        _isWarningCompletionActive = true;
                        break;
                    case Automate.StateEngine.ProcessState.Connected:
                        if (request)
                        {
                            request = false;
                            AutoCompleteResponse aResponse = _currentConnector.execute<AutoCompleteAndReferenceRequest>(aRequest, ref _currentInvocationId, Constants.SYNCHRONOUS_COMMANDS_TIMEOUT) as AutoCompleteResponse;

                            if (aResponse == null)
                            {
                                _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_AUTO_COMPLETION_NULL_RESP, Properties.Resources.ERR_AUTO_COMPLETION_NULL_DESC));
                                _isWarningCompletionActive = true;
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
                                            var option = aResponse.options.First(); 
                                            //auto insert
                                            SelectedCompletion = new Completion(option.display, option.insert, option.desc, Completion.AutoCompletionType.Label);
                                            CharProcessAction  = CharProcessResult.ForceCommit;
                                        }
                                        else
                                        {
                                            _completionList.AddRange(aResponse.options.Select(x => new Completion(x.display, x.insert, x.desc, Completion.AutoCompletionType.Label)).OrderBy(x => x.InsertionText));
                                        }
                                    }
                                }
                                Filter();
                                _isWarningCompletionActive = false;
                            }
                        }                        
                        break;
                    default:
                        Logger.Instance.Append(Logger.MessageType.FatalError, _currentConnector.Workspace, "Undefined connector state reached. Please notify support.");
                        _isWarningCompletionActive = true;
                        break;

                }
            }
            else
            {
                _completionList.Add(CreateWarningCompletion(Properties.Resources.CONNECTOR_INSTANCE_NULL, Properties.Resources.CONNECTOR_INSTANCE_NULL_DESC));
            }
        }

        public void Filter()
        {
            string fallBackHint  = _previousHint;
            var previousSelection = SelectedCompletion;
            
            if(TriggerPoint.HasValue && !String.IsNullOrWhiteSpace(TriggerPoint.Value.Context))
            {
                _previousHint = TriggerPoint.Value.Context;
                _filteredList.Filter(x => x.InsertionText.StartsWith(_previousHint, StringComparison.OrdinalIgnoreCase));
                               
                
                if(_filteredList.Count == 0)
                {
                    _filteredList.Filter(x => x.InsertionText.Contains(_previousHint, StringComparison.OrdinalIgnoreCase));
                    if(_filteredList.Count == 0)
                    {
                        //if count is null - previous selection is no longer valid!

                        //fuzzy matching
                        _filteredList.StopFiltering();
                        if (previousSelection != null)
                        {
                            previousSelection.IsFuzzy = true;
                            previousSelection.IsSelected = false;
                        }
                    }
                    else
                    {
                        if (previousSelection != null)
                        {
                            previousSelection.IsFuzzy = previousSelection.IsSelected = false;
                        }
                        SelectedCompletion = _filteredList.Aggregate((curMin, x) => (x.InsertionText.Length > curMin.InsertionText.Length ? x : curMin));
                        SelectedCompletion.IsSelected = SelectedCompletion.IsFuzzy = true;
                    }
                }
                else
                {
                    if (previousSelection != null)
                    {
                        previousSelection.IsFuzzy = previousSelection.IsSelected = false;
                    }
                    //select the entry with minimum length                    
                    SelectedCompletion = _filteredList.Aggregate((curMin, x) => (x.InsertionText.Length > curMin.InsertionText.Length ? x : curMin));                
                    SelectedCompletion.IsSelected = SelectedCompletion.IsFuzzy = true;
                }
            }
            else
            {
                _filteredList.StopFiltering();
                if(previousSelection != null)
                {
                    previousSelection.IsSelected = previousSelection.IsFuzzy = false;
                }
                SelectedCompletion            = _filteredList.First();
                SelectedCompletion.IsFuzzy    = true;
                SelectedCompletion.IsSelected = false;
            }            
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
        private readonly BulkObservableCollection<Completion> _completionList   = new BulkObservableCollection<Completion>(); //!< Underlying completion list.
        private readonly FilteredObservableCollection<Completion> _filteredList = null;                                       //!< UI completion list based on underlying list.
        private Tokenizer.TokenTag? _triggerToken                               = null;                                       //!< Current completion list trigger token.
        private Connector _currentConnector                                     = null;                                       //!< Connector for current document.
        private MatchingType _lastMatchingType                                  = MatchingType.NONE;                          //!< Last matching completion type.
        private int _currentInvocationId                                        = -1;                                         //!< Auto completion invocation id.
        private int _count                                                      = 0;                                          //!< Options count.
        private double _zoomLevel                                               = 1.0;                                        //!< Auto completion window zoom level.
        private Completion _selectedCompletion                                  = null;                                       //!< Currently selected option.
        private string _previousHint                                            = String.Empty;                               //!< Last string which matched an auto completion option.
        private List<Completion> _cachedOptions                                 = null;                                       //!< Last set of options.
        private bool _isWarningCompletionActive                                 = false;                                      //!< Indicates if a warning option was active.
        #endregion
    }
}
