using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using CSScriptIntellisense;
using RTextNppPlugin.Automate;
using RTextNppPlugin.Automate.Protocol;
using RTextNppPlugin.Logging;
using RTextNppPlugin.Parsing;
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

        public Tokenizer.TokenTag? TriggerPoint { get; private set; }

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
            CharProcessAction = CharProcessResult.NoAction;
            IsSelected        = false;
            IsUnique          = false;
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

        public ObservableCollection<Completion> CompletionList
        {
            get
            {
                return _completionList;
            }
        }

        public void AugmentAutoCompletion(ContextExtractor extractor, Point caretPoint, Tokenizer.TokenTag ? token, ref bool request)
        {
            _triggerToken = token;
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
                                try
                                {
                                    //check invocation id
                                    if (_currentInvocationId != aResponse.invocation_id)
                                    {
                                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_AUTO_COMPLETION_INVOKATION, Properties.Resources.ERR_AUTO_COMPLETION_INVOKATION_DESC));
                                        Logger.Instance.Append(  Logger.MessageType.Error, 
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
                                                //if we are here, it means that the response is succcesful
                                                foreach (var option in aResponse.options)
                                                {
                                                    _completionList.Add(new Completion(option.display, option.insert, option.desc, Completion.AutoCompletionType.Label));
                                                }

                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Instance.Append(Logger.MessageType.Error,
                                                           _currentConnector.Workspace,
                                                           String.Format("Auto complete for file {0} failed. Exception {1}.", Npp.GetCurrentFile(), ex.Message));
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
            Trace.WriteLine("Filtering...");
        }
        #endregion

        #region [Helpers]
        Completion CreateWarningCompletion(string warning, string desc)
        {
            return new Completion(warning, null, desc, Completion.AutoCompletionType.Warning);
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
                _triggerToken = aTokenizer.TriggerToken;
                if(!_triggerToken.HasValue)
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
        private readonly ObservableCollection<Completion> _completionList = new ObservableCollection<Completion>();
        private Tokenizer.TokenTag? _triggerToken = null;
        private Connector _currentConnector = null;
        private int _currentInvocationId = -1;
        private int _count = 0;
        private double _zoomLevel = 1.0;
        #endregion
    }
}
