﻿using CSScriptIntellisense;
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
            FilteredCount      = 0;            
            CharProcessAction  = CharProcessResult.NoAction;
            SelectedCompletion = null;
            _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC));       
        }

        public void OnAutoCompletionWindowCollapsing()
        {
            _cachedOptions = new List<Completion>(_completionList);
            _completionList.Clear();
            _filteredList.StopFiltering();
            SelectedCompletion = null;
        }

        public BulkObservableCollection<Completion> UnderlyingList
        {
            get { return _completionList; } 
        }

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

        public void ClearSelectedCompletion()
        {
            SelectedCompletion = null;
        }

        public FilteredObservableCollection<Completion> CompletionList
        {
            get
            {
                return _filteredList;
            }
        }

        public void AugmentAutoCompletion(ContextExtractor extractor, Point caretPoint, AutoCompletionTokenizer tokenizer, ref bool request)
        {
            CharProcessAction = CharProcessResult.NoAction;
            _completionList.Clear();
            if ((TriggerPoint.HasValue && tokenizer.TriggerToken.HasValue && TriggerPoint.Value == tokenizer.TriggerToken.Value && _cachedOptions != null) && !_isWarningCompletionActive && !request)
            {
                TriggerPoint = tokenizer.TriggerToken;
                _completionList.AddRange(_cachedOptions);
                Filter();
                return;
            }

            TriggerPoint = tokenizer.TriggerToken;            
            if (!tokenizer.TriggerToken.HasValue)
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
                                //add pics, remove existins options
                                var filteredList = aResponse.options.AsParallel().Where(x => !tokenizer.LineTokens.Contains(x.insert));
                                var labeledList = filteredList.Select(x => new Completion(
                                    x.display,
                                    x.insert,
                                    x.desc,
                                    x.insert.Contains("event", StringComparison.InvariantCultureIgnoreCase) ? Completion.AutoCompletionType.Event : Completion.AutoCompletionType.Label));

                                if (labeledList.Count() != 0)
                                {
                                    if (labeledList.Count() == 1)
                                    {
                                        //auto insert
                                        SelectedCompletion            = labeledList.First();
                                        SelectedCompletion.IsSelected = true;
                                        CharProcessAction             = CharProcessResult.ForceCommit;
                                    }
                                    else
                                    {
                                        _completionList.AddRange(labeledList.OrderBy(x => x.InsertionText));
                                        Filter();
                                    }
                                }
                                else
                                {
                                    CharProcessAction = CharProcessResult.ForceClose;
                                }
                            }
                            _isWarningCompletionActive = false;
                            request                    = false;
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
            var previousSelection = SelectedCompletion;
            if (previousSelection != null)
            {
                previousSelection.IsSelected = previousSelection.IsFuzzy = false;
            }
            
            if(TriggerPoint.HasValue && !String.IsNullOrWhiteSpace(TriggerPoint.Value.Context))
            {
                _previousHint = TriggerPoint.Value.Context;
                _filteredList.Filter(x => x.InsertionText.StartsWith(_previousHint, StringComparison.OrdinalIgnoreCase) || x.InsertionText.Contains(_previousHint, StringComparison.OrdinalIgnoreCase));

                if ((FilteredCount = _filteredList.Count) == 0)
                {
                    //if count is null - previous selection is no longer valid!
                    //fuzzy matching
                    _filteredList.StopFiltering();
                    if (previousSelection != null)
                    {
                        SelectedIndex = _filteredList.IndexOf(previousSelection);
                    }

                    if(SelectedIndex == -1)
                    {
                        SelectedCompletion = _filteredList.First();
                        SelectedIndex      = 0;
                    }

                    SelectedCompletion.IsFuzzy    = true;
                    SelectedCompletion.IsSelected = false;

                    FilteredCount = _completionList.Count;                    
                }
                else
                {
                    //select with priority to prefix
                    SelectedCompletion = _filteredList.Where(x => x.InsertionText.StartsWith(_previousHint, StringComparison.OrdinalIgnoreCase)).OrderByDescending( x=> x.InsertionText.Length).FirstOrDefault();
                    if(SelectedCompletion == null)
                    {
                        //match is contained
                        SelectedCompletion = _filteredList.Where(x => x.InsertionText.Contains(_previousHint, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.InsertionText.Length).First();
                    }
                    SelectedIndex = _filteredList.IndexOf(SelectedCompletion);

                    SelectedCompletion.IsSelected = SelectedCompletion.IsFuzzy = true;
                }
            }
            else
            {
                _filteredList.StopFiltering();
                SelectedCompletion            = _filteredList.First();
                SelectedCompletion.IsFuzzy    = true;
                SelectedCompletion.IsSelected = false;
                FilteredCount = _completionList.Count;
                SelectedIndex = -1;
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
            int aCurrentPosition = CSScriptIntellisense.Npp.GetCaretPosition();
            int aLineNumber      = CSScriptIntellisense.Npp.GetLineNumber();
            if(!_triggerToken.HasValue)
            {
                CharProcessAction = CharProcessResult.ForceClose;
                return;
            }
            Tokenizer.TokenTag t = TriggerPoint.Value;
            string aContext = t.Context;
            bool wasEmpty = (aContext.Length == 0);
            if (char.IsWhiteSpace(c) && (wasEmpty || t.Type == RTextTokenTypes.Comma ))
            {
                CharProcessAction = CharProcessResult.MoveToRight;                
                //if auto completion is inside comment, notation, name, string jusr return
                AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(aLineNumber, aCurrentPosition, Npp.GetColumn());
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
        private int _selectedIndex                                              = 0;                                          //!< Indicates the selected index of the filtered option list.
        private bool _isFiltering                                               = false;                                      //!< Indicates if filtering function is currently active.
        #endregion
    }
}
