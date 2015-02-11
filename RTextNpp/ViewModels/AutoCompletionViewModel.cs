using RTextNppPlugin.Parsing;
using RTextNppPlugin.WpfControls;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Automate.Protocol;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using RTextNppPlugin.Automate;
using RTextNppPlugin.Forms;
using RTextNppPlugin.Logging;
using System.Diagnostics;
using CSScriptIntellisense;

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
            MoveLeft  = false;
            MoveRight = false;            
            switch(c)
            {
                case Constants.BACKSPACE:
                    //special treatment here..
                    break;          
                default:                    
                    //just add/insert character 
                    AddCharToTriggerPoint(c);
                    break;
            }
            Trace.WriteLine(String.Format("OnKeyPressed token \n{0}", _triggerToken.Value));
        }

        public bool MoveLeft { get; private set; }

        public bool MoveRight { get; private set; }

        public bool IsSelected { get; private set; }

        public bool IsUnique { get; private set; }

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
            MoveLeft   = false;
            MoveRight  = false;
            IsSelected = false;
            IsUnique   = false;
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
                        Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, _currentConnector.Workspace, "Undefined connector state reached. Please notify support.");
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
            //char need to be inserted at previous caret column - token needs to be updated
            if(_triggerToken.HasValue)
            {
                Tokenizer.TokenTag t = _triggerToken.Value;
                string aContext = t.Context;
                bool wasEmpty = (aContext.Length == 0);
                if (wasEmpty && Char.IsWhiteSpace(c))
                {
                    MoveRight = true;                    
                }
                else
                {
                    if (t.CaretColumn == t.EndColumn)
                    {
                        //caret at end of token - just add char at the end
                        aContext += Char.ToString(c);
                    }
                    else
                    {
                        //care is someone inside the token -> insert character
                        int aInsertColumn = t.EndColumn - t.CaretColumn;
                        aContext.Insert(aInsertColumn, Char.ToString(c));
                    }
                }
                //move columns etc one to the right
                _triggerToken = new Tokenizer.TokenTag
                {
                    EndColumn = t.EndColumn + 1,
                    StartColumn = wasEmpty ? t.EndColumn : t.StartColumn,
                    Type = t.Type,
                    Line = t.Line,
                    BufferPosition = wasEmpty ? t.EndColumn : t.BufferPosition,
                    Context = aContext,
                    CaretColumn = t.CaretColumn + 1
                };
            }
            else
            {
                Trace.WriteLine("\n\n#######\n\nERROR : Trigger point has no value \n\n#######\n\n");
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
