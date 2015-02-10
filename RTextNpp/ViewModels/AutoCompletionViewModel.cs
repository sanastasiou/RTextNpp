using RTextNppPlugin.Parsing;
using RTextNppPlugin.WpfControls;
using RTextNppPlugin.Utilities.Protocol;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using RTextNppPlugin.Automate;
using RTextNppPlugin.Forms;

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
        public AutoCompletionViewModel()
        {
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public AutoCompletionForm Host { get; set; }

        public void Filter(Tokenizer.TokenTag? token)
        {
            _currentToken = token;             
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

        public void foo()
        {
            _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC));
            Count = _completionList.Count;
        }

        public void AugmentAutoCompletion(ContextExtractor extractor, Point caretPoint, Tokenizer.TokenTag ? token, ref bool request)
        {
            AutoCompleteAndReferenceRequest aRequest = new AutoCompleteAndReferenceRequest
            {
                column        = extractor.ContextColumn,
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
                            request = false;

                        }
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC));
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC));
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC));
                        _completionList.Add(CreateWarningCompletion(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC));
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
            ////_completionList.Clear();
            ////foreach(var o in options)
            ////{
            ////    _completionList.Add(o);
            ////}
            //_currentToken = token;
            Host.ResizeToWpfSize();
        }
        #endregion

        #region [Helpers]
        Completion CreateWarningCompletion(string warning, string desc)
        {
            return new Completion(warning, null, desc, Completion.AutoCompletionType.Warning);
        }

        #endregion

        #region [Data Members]
        private readonly ObservableCollection<Completion> _completionList = new ObservableCollection<Completion>();
        private Tokenizer.TokenTag? _currentToken = null;
        private Connector _currentConnector = null;
        private int _currentInvocationId = -1;
        private int _count = 0;
        #endregion
    }
}
