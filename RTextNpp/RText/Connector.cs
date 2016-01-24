using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using RTextNppPlugin.RText.Protocol;
using RTextNppPlugin.RText.StateEngine;
using RTextNppPlugin.Utilities;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace RTextNppPlugin.RText
{
    /**
     * \class   Connector
     *
     * \brief   Connector. All commands are going through a connector instance.
     *
     */
    internal class Connector : IConnector
    {
        #region [Data Members]
        private ManualResetEvent _receivedResponseEvent                    = new ManualResetEvent(false)                     ;            //!< The received response event
        private int _InvocationId                                          = 0                                               ;            //!< Identifier for the invocation
        private SocketConnection _connection                               = new SocketConnection()                          ;            //!< The receive status, used to store a state between calls of the receive callback
        private readonly Regex _messageLengthRegex                         = new Regex(@"^(\d+)\{", RegexOptions.Compiled)   ;            //!< The message length regular expression
        private IConnectorState _currentState                                                                                ;            //!< Indicates the current connector state.
        private string _activeCommand                                                                                        ;            //!< Indicates the currently executing command
        private RTextBackendProcess _backendProcess                                                                          ;            //!< Indicates the back-end process
        private CancellationTokenSource _receivingThreadCancellationSource = null                                            ;            //!< Used to cancel a synchronous receiving thread without waiting for the timeout to complete.
        private IResponseBase _lastResponse                                = null                                            ;            //!< Holds the last response from the back-end.
        public readonly RequestBase LOAD_COMMAND                           = new RequestBase { command = Constants.Commands.LOAD_MODEL }; //!< Load command.
        private bool _cancelled                                            = false;                                                       //!< Indicates that a pending command was canceled via user request.
        private LoadResponse _currentLoadResponse                          = default(LoadResponse);                                       //!< Indicates last load response.
        #endregion
        
        #region Interface
        /**
         * \struct  ProgressResponseStruct
         *
         * \brief   Progress response structure. Maps a command with a progress response message.
         *
         */
        
        public struct ProgressResponseEventArgs
        {
            public ProgressResponse Response;
            public String Command;
            public String Workspace;
        }
        
        public string Workspace { get { return _backendProcess.ProcKey; } }
        
        public bool IsCommandCancelled { get { return _cancelled; } }
        
        public delegate void ProgressUpdatedEvent(object source, ProgressResponseEventArgs e);
        
        public event ProgressUpdatedEvent OnProgressUpdated;
        
        public delegate void StateChangedEvent(object source, StateChangedEventArgs e);
        
        public event StateChangedEvent OnStateChanged;
        
        public class StateChangedEventArgs : EventArgs
        {
            public ConnectorStates StateLeft { get; private set; }
            public ConnectorStates StateEntered { get; private set; }
            public String Workspace { get; private set; }
            public String Command { get; private set; }
            public StateChangedEventArgs(ConnectorStates stateLeft, ConnectorStates stateEntered, string workspace, string command)
            {
                StateLeft     = stateLeft;
                StateEntered  = stateEntered;
                Workspace     = workspace;
                Command       = command;
            }
        }

        public string ActiveCommand
        {
            get
            {
                return _activeCommand;
            }
            set
            {
                if(_activeCommand != value)
                {
                    _activeCommand = value;
                }                
            }
        }
        
        /**
         * \class   CommandCompletedEventArgs
         *
         * \brief   Command completed is an event which is fired to notify subscribers that an async command has finished executing.
         *
         */
        public class CommandCompletedEventArgs : EventArgs
        {
            /**
             * \property    public IResponseBase Response
             *
             * \brief   Gets or sets the JSON response.
             *
             * \return  The response.
             */
            public IResponseBase Response { get; private set; }
            
            /**
             * \property    public int InvocationId
             *
             * \brief   Gets or sets the identifier of the invocation.
             *
             * \return  The identifier of the invocation.
             */
            public int InvocationId { get; private set; }
            
            /**
             * \property    public string ResponseType
             *
             * \brief   Gets or sets the type of the response.
             *
             * \return  The type of the response.
             */
            public string ResponseType { get; private set; }
            
            /**
             *
             * \brief   Constructor.
             *
             *
             * \param   response        The response.
             * \param   invocationId    Identifier for the invocation.
             */
            public CommandCompletedEventArgs(IResponseBase response, int invocationId, string type )
            {
                Response     = response;
                InvocationId = invocationId;
                ResponseType = type;
            }
        }
        public IConnectorState CurrentState
        {
            get
            {
                return _currentState;
            }
            set
            {
                if(value != _currentState)
                {
                    _currentState = value;
                }
            }
        }
        
        /**
         * \brief   Constructor.
         *
         * \param   proc    Back-end process instance for this connector.
         *
         */
        public Connector( RTextBackendProcess proc) : base()
        {
            _backendProcess = proc;
            _currentState   = new Disconnected(this);
            _backendProcess.ProcessExitedEvent += ProcessExitedEvent;
        }
        
        void ProcessExitedEvent(object source, RTextBackendProcess.ProcessExitedEventArgs e)
        {
            //kill any ongoing commands
            if (IsBusy() && _receivingThreadCancellationSource != null)
            {
                _receivingThreadCancellationSource.Cancel();
            }
            _currentState.ExecuteCommand(StateEngine.Command.Disconnected);
            //reset invocation id counter since the back-end process died or exited after an idle timeout
            _InvocationId = 0;
        }
        
        public async Task<IResponseBase> ExecuteAsync<Command>(Command command, int timeout, StateEngine.Command cmd) where Command : RequestBase
        {
            if (!IsBusy())
            {
                UpdateFsmAndPendingCommand(command, cmd);
                return await SendAsync<Command>(command, timeout);
            }
            else
            {
                bool isProcessStarted = await RestartService();
                if (isProcessStarted)
                {
                    EnsureModelIsLoaded();
                }
                return null;
            }
        }
        
        /**
         * \brief   Executes the given command synchronously.
         *
         * \tparam  Command Type of the command.
         * \param   command                 The command.
         * \param   [in,out]  invocationId    Identifier for the invocation.
         * \param   timeout                 (Optional) the timeout.
         *
         * \return  A Response type instance if the command could be executed successfully and within the
         *          provided timeout, else null.
         *
         * \note    Users of this function must be prepared to receive null, as indication that
         *          something went wrong.
         */
        public async Task BeginExecute<Command>(Command command, StateEngine.Command cmd) where Command : RequestBase
        {
            if (!IsBusy())
            {
                UpdateFsmAndPendingCommand(command, cmd);
                BeginSend<Command>(command);
            }
            else
            {
                bool isProcessStarted = await RestartService();
                if (isProcessStarted)
                {
                    EnsureModelIsLoaded();
                }
            }
        }
        
        internal void CancelCommand()
        {
            _cancelled = true;
            if (_receivingThreadCancellationSource != null)
            {
                _receivingThreadCancellationSource.Cancel();
            }
        }
        #endregion
        
        internal string LogChannel
        {
            get
            {
                return _backendProcess.Workspace;
            }
        }
        
        internal LoadResponse ErrorList
        {
            get
            {
                return _currentLoadResponse;
            }
            private set
            {
                _currentLoadResponse = value;
            }
        }
        
        #region Helpers
        
        /**
         * Begins an async send. Just send a command without caring about the response of the back-end.
         * Callers of this function should subscribe to CommandExecuted event to be able to receive a
         * response. Usually used for long running commands like load_model.
         *
         * \tparam  Command Type of the command.
         * \param   command The command.
         *
         */
        private void BeginSend<Command>(Command command) where Command : RequestBase
        {
            bool aHasErrorOccured = false;
            try
            {
                byte[] msg = GetCommandAsByteArray(command);
                if (_connection.SendRequest(msg) != msg.Length)
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error,
                                                    _backendProcess.Workspace,
                                                    "void BeginSend<Command>(ref Command command, ref int invocationId) - Could not send request {0 }. Timeout of {1} has expired.", command.command, Constants.SEND_TIMEOUT);
                    aHasErrorOccured = true;
                }
            }
            catch (ArgumentNullException ex)
            {
                aHasErrorOccured = true;
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, _backendProcess.Workspace, "void BeginSend<Command>(ref Command command, ref int invocationId) - ArgumentNullException : {0}", ex.ToString());
            }
            catch (SocketException ex)
            {
                aHasErrorOccured = true;
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, _backendProcess.Workspace, "void BeginSend<Command>(ref Command command, ref int invocationId) - SocketException : {0}", ex.ToString());
            }
            catch (Exception ex)
            {
                aHasErrorOccured = true;
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, _backendProcess.Workspace, "void BeginSend<Command>(ref Command command, ref int invocationId) - Exception : {0}", ex.ToString());
            }
            if (aHasErrorOccured)
            {
                _currentState.ExecuteCommand(StateEngine.Command.Disconnected);
            }
        }
        
        /**
         * Sends a command while waiting asynchronously for a response.
         *
         * \tparam  Command Type of the command.
         * \param   command The command.
         * \param   timeout The timeout to wait after which the command will be responded with null.
         *
         * \return  The response from the back-end.
         */
        private async Task<IResponseBase> SendAsync<Command>(Command command, int timeout) where Command : RequestBase
        {
            try
            {
                byte[] msg = GetCommandAsByteArray(command);
                // Send the data through the socket.
                //wait for manual reset event which indicates that the response has arrived
                _receivedResponseEvent.Reset();
                _receivingThreadCancellationSource = new CancellationTokenSource();
                Task<IResponseBase> errorTask = new Task<IResponseBase>(new Func<IResponseBase>(() =>
                {
                    while (true)
                    {
                        //wait 0.05 seconds
                        Thread.Sleep(50);
                        // Check is task should be ended!
                        if (_receivingThreadCancellationSource.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    return null;
                }), _receivingThreadCancellationSource.Token);
                Task<IResponseBase> receiverTask = new Task<IResponseBase>(new Func<IResponseBase>(() =>
                {
                    if (!_receivedResponseEvent.WaitOne(timeout))
                    {
                        _currentState.ExecuteCommand(StateEngine.Command.Disconnected);
                        _lastResponse = null;
                    }
                    return _lastResponse;
                }));
                receiverTask.Start();
                errorTask.Start();
                if (_connection.SendRequest(msg) != msg.Length)
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error,
                                                    _backendProcess.Workspace,
                                                    "Could not send request to RTextService."
                                                  );
                    _currentState.ExecuteCommand(StateEngine.Command.Disconnected);
                    return null;
                }
                var result = await Task.WhenAny<IResponseBase>(receiverTask, errorTask);
                if (!_receivingThreadCancellationSource.IsCancellationRequested)
                {
                    _receivingThreadCancellationSource.Cancel();
                }
                return result.Result;
            }
            catch (ArgumentNullException ex)
            {
                _currentState.ExecuteCommand(StateEngine.Command.Disconnected);
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, _backendProcess.Workspace, "void send<Command>(ref Command command, ref int invocationId, int timeout) - Exception : {0}", ex.Message);
                return null;
            }
        }
        
        /**
        *
        * \brief   Tries to connect to a remote end point. If it succeeds signals main thread and sets a flag that the connection is successful.
        *
        * \param ar THe async result
        */
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the connection.
                _connection.EndConnect(ar);
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, _backendProcess.Workspace, "void ConnectCallback(IAsyncResult ar) - Exception : {0}", ex.ToString());
            }
        }
        
        /**
         *
         * \brief   Callback for asynchronous receive functionality.
         *
         * \param   ar  The async result.
         */
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Read data from the remote device.
                _connection.EndReceive(ar);
                if (_connection.BytesToRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    _connection.Append();
                    // converts string into json objects
                    TryDeserialize();
                    //fall through is connection is terminated
                    if (_connection.Connected)
                    {
                        _connection.BeginReceive(new AsyncCallback(ReceiveCallback));
                    }
                }
            }
            catch (Exception ex)
            {
                _currentState.ExecuteCommand(StateEngine.Command.Disconnected);
                Logging.Logger.Instance.Append( Logging.Logger.MessageType.Error,
                                                _backendProcess.Workspace,
                                                "Could not receive response from RTextService.\nException : {0}",
                                                ex.Message);
            }
        }
        
        /**
         * \brief   Query if this object is busy.
         *
         * \return  true if busy, false if not.
         */
        private bool IsBusy()
        {
            return _currentState.State != ConnectorStates.Idle;
        }
        
        /**
        *
        * \brief   Recursively deserialize JSON messages.
        *
        */
        private void TryDeserialize()
        {
            if (_connection.LengthMatched)
            {
                //we know the length but the received stream is not enough
                if (_connection.RequiredLength <= _connection.ReceivedMessage.Length)
                {
                    OnSufficientResponseLengthAcquired();
                }
            }
            else
            {
                Match aMatch = _messageLengthRegex.Match(_connection.ReceivedMessage.ToString());
                if (aMatch.Success)
                {
                    _connection.LengthMatched   = true;
                    _connection.JSONLength      = Int32.Parse(aMatch.Groups[1].Value);
                    _connection.RequiredLength  = _connection.JSONLength.ToString().Length + _connection.JSONLength;
                    _connection.ReceivedMessage = new StringBuilder(_connection.ReceivedMessage.ToString(), _connection.RequiredLength);
                    if (_connection.RequiredLength <= _connection.ReceivedMessage.Length)
                    {
                        OnSufficientResponseLengthAcquired();
                    }
                }
            }
        }
        
        private void OnSufficientResponseLengthAcquired()
        {
            _connection.LengthMatched   = false;
            string aJSONmessage         = _connection.ReceivedMessage.ToString(_connection.JSONLength.ToString().Length, _connection.JSONLength);
            _connection.ReceivedMessage = new StringBuilder(_connection.ReceivedMessage.ToString(_connection.RequiredLength,
                                                            _connection.ReceivedMessage.Length - _connection.RequiredLength));
            //handle various responses
            AnalyzeResponse(aJSONmessage);
            TryDeserialize();
        }
        
        /**
         *
         * \brief   Analyzes the last received response.
         *
         * \param   response    The response string.
         */
        private void AnalyzeResponse(string response)
        {
            bool aIsResponceReceived = false;
            switch (ActiveCommand)
            {
                case Constants.Commands.LOAD_MODEL:
                    _lastResponse = JsonConvert.DeserializeObject<LoadResponse>(response) as IResponseBase;
                    aIsResponceReceived = IsNotResponseOrErrorMessage();
                    break;
                case Constants.Commands.LINK_TARGETS:
                    _lastResponse = JsonConvert.DeserializeObject<LinkTargetsResponse>(response) as IResponseBase;
                    aIsResponceReceived = IsNotResponseOrErrorMessage();
                    break;
                case Constants.Commands.FIND_ELEMENTS:
                    _lastResponse = JsonConvert.DeserializeObject<FindRTextElementsResponse>(response) as IResponseBase;
                    aIsResponceReceived = IsNotResponseOrErrorMessage();
                    break;
                case Constants.Commands.CONTENT_COMPLETION:
                    _lastResponse = JsonConvert.DeserializeObject<AutoCompleteResponse>(response) as IResponseBase;
                    aIsResponceReceived = IsNotResponseOrErrorMessage();
                    break;
                case Constants.Commands.CONTEXT_INFO:
                    _lastResponse = JsonConvert.DeserializeObject<ContextInfoResponse>(response) as IResponseBase;
                    aIsResponceReceived = IsNotResponseOrErrorMessage();
                    break;
                case Constants.Commands.STOP:
                    _lastResponse = JsonConvert.DeserializeObject<ResponseBase>(response) as IResponseBase;
                    aIsResponceReceived = true;
                    _connection.CleanUpSocket();
                    break;
            }
            if (aIsResponceReceived)
            {
                if (ActiveCommand == Constants.Commands.LOAD_MODEL)
                {
                    ErrorList = (_lastResponse as LoadResponse);
                }
                if (_InvocationId - 1 != _lastResponse.invocation_id)
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error,
                                                    _backendProcess.Workspace,
                                                    "void AnalyzeResponse(ref string response, ref StateObject state) - Invocation id mismatch : Expected {0} - Received {1}",
                                                    _InvocationId - 1,
                                                    _lastResponse.invocation_id);
                    _lastResponse = null;
                    _currentState.ExecuteCommand(StateEngine.Command.Disconnected);
                }
                else
                {
                    _receivedResponseEvent.Set();
                    if (ActiveCommand != Constants.Commands.STOP)
                    {
                        _currentState.ExecuteCommand(StateEngine.Command.ExecuteFinished);
                    }
                }
            }
        }
        
        /**
         *
         * \brief   Updates the progress described by response.
         *
         * \return  true if this is not a progress message, false otherwise.
         */
        private bool IsNotResponseOrErrorMessage()
        {
            switch (_lastResponse.type)
            {
                case Constants.Commands.PROGRESS:
                    if (OnProgressUpdated != null)
                    {
                        OnProgressUpdated(this, new ProgressResponseEventArgs
                        {
                            Response  = (ProgressResponse)_lastResponse,
                            Command   = ActiveCommand,
                            Workspace = _backendProcess.ProcKey
                        });
                    }
                    _receivedResponseEvent.Reset();
                    return false;
                case Constants.Commands.ERROR:
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, _backendProcess.Workspace, "bool UpdateProgress( IResponseBase response ) - Back-end reports unknown command error.");
                    return false;
                default:
                    return true;
            }
        }
        
        private byte[] PrepareRequestString( string serializedCommand )
        {
            StringBuilder aExtendedString = new StringBuilder(serializedCommand.Length + 10);
            aExtendedString.Append(serializedCommand.Length);
            aExtendedString.Append(serializedCommand);
            return Encoding.ASCII.GetBytes(aExtendedString.ToString());
        }
        
        private async Task<bool> RestartService()
        {
            if (_backendProcess.HasExited)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, _backendProcess.Workspace, "RTextService is not running. Trying to restart service...");
            }
            bool aBackendInitialized = await _backendProcess.InitializeBackendAsync();
            return aBackendInitialized;
        }
        
        private void EnsureModelIsLoaded()
        {
            if (CurrentState.State == ConnectorStates.Disconnected)
            {
                _currentState.ExecuteCommand(Command.Connect);
            }
        }
        
        private void UpdateFsmAndPendingCommand<Command>(Command command, StateEngine.Command cmd) where Command : RequestBase
        {
            _cancelled     = false;
            ActiveCommand  = command.command;
            _currentState.ExecuteCommand(cmd);
        }
        
        private byte[] GetCommandAsByteArray<Command>(Command command) where Command : RequestBase
        {
            command.invocation_id = _InvocationId++;
            JsonConvert.SerializeObject(command, Formatting.None);
            return PrepareRequestString(JsonConvert.SerializeObject(command, Formatting.None));
        }
        #endregion
        
        #region IConnector Members
        
        public void OnDisconnectedEntry()
        {
            //will be "cleaned" by process exit event, also need to keep connection alive till back-end acknowledges stop command
            if (ActiveCommand != Constants.Commands.STOP)
            {
                _connection.CleanUpSocket();
            }
        }
        
        public void OnConnectingEntry()
        {
            try
            {
                //will throw if something is wrong
                IAsyncResult aConnectedResult = _connection.BeginConnect("localhost", _backendProcess.Port, ConnectCallback);
                if (!aConnectedResult.AsyncWaitHandle.WaitOne(Constants.CONNECT_TIMEOUT))
                {
                    _currentState.ExecuteCommand(StateEngine.Command.Disconnected);
                }
                else
                {
                    //start receiving
                    _connection.BeginReceive(new AsyncCallback(ReceiveCallback));
                    _currentState.ExecuteCommand(StateEngine.Command.Connected);
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, _backendProcess.Workspace, "void Connect() - Exception : {0}", ex.ToString());
            }
        }
        
        public void OnLoadingEntry()
        {
            _cancelled     = false;
            ActiveCommand = LOAD_COMMAND.command;
            BeginSend(LOAD_COMMAND);
            ErrorList      = default(LoadResponse);
        }
        
        public void OnStateLeft(ConnectorStates oldState, ConnectorStates newState)
        {
            if (OnStateChanged != null)
            {
                OnStateChanged(this, new StateChangedEventArgs(oldState, newState, _backendProcess.ProcKey, ActiveCommand));
            }
        }

        public void OnIdleEntry()
        {
            ActiveCommand = string.Empty;
        }
        #endregion
    }
}