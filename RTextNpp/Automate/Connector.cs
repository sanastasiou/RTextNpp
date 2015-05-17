using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Jil;
using RTextNppPlugin.Automate.Protocol;
using RTextNppPlugin.Automate.StateEngine;
using RTextNppPlugin.Utilities;
using System.Threading.Tasks;


namespace RTextNppPlugin.Automate
{
    /**
     * \class   Connector
     *
     * \brief   Connector. All commands are going through a connector instance.
     *
     */
    public class Connector
    {
        #region [Data Members]        
        private IPHostEntry mIpHostInfo                                    = Dns.GetHostEntry("localhost")                   ;            //!< Information describing the IP host
        private ManualResetEvent mReceivedResponseEvent                    = new ManualResetEvent(false)                     ;            //!< The received response event 
        private int mInvocationId                                          = 0                                               ;            //!< Identifier for the invocation
        private SocketConnection mConnection                               = new SocketConnection()                          ;            //!< The receive status, used to store a state between calls of the receive callback
        private Regex mMessageLengthRegex                                  = new Regex(@"^(\d+)\{", RegexOptions.Compiled)   ;            //!< The message length regular expression
        private StateMachine mFSM                                                                                            ;            //!< The fsm
        private string mActiveCommand                                                                                        ;            //!< Indicates the currently executing command
        private RTextBackendProcess mBackendProcess                                                                          ;            //!< Indicates the backend process
        private CancellationTokenSource mReceivingThreadCancellationSource = null                                            ;            //!< Used to cancel a synchronous receiving thread without waiting for the timeout to complete.
        private IResponseBase mLastResponse                                = null                                            ;            //!< Holds the last response from the backend.
        private int mLastInvocationId                                      = -1                                              ;            //!< Holds the last invocation id from the backend.
        private readonly RequestBase LOAD_COMMAND                          = new RequestBase { command = Constants.Commands.LOAD_MODEL }; //!< Load command.
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

        public delegate void ProgressUpdatedEvent(object source, ProgressResponseEventArgs e);

        public event ProgressUpdatedEvent OnProgressUpdated;

        public string Workspace
        {
            get
            {
                return mBackendProcess.ProcKey;
            }
        }

        /**
         *
         * \brief   Delegate for the CommandCompleted event.
         *
         *
         * \param   source  Source object of the event.
         * \param   e       Command completed event information.
         */
        public delegate void CommandExecuted(object source, CommandCompletedEventArgs e);

        public event CommandExecuted OnCommandExecuted;                                   //!< Event queue for all listeners interested in CommmandExecuted events.

        public delegate void StateChangedEvent(object source, StateChangedEventArgs e);

        public event StateChangedEvent OnStateChanged;

        public class StateChangedEventArgs : EventArgs
        {
            public ProcessState State { get; private set; }

            public String Workspace { get; private set; }
            public String Command { get; private set; }

            public StateChangedEventArgs(ProcessState state, string workspace, string command)
            {
                State     = state;
                Workspace = workspace;
                Command   = command;
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

        /**
         * \brief   Constructor.
         *
         * \param   proc    Backend process instance for this connector.
         *
         */        
        public Connector( RTextBackendProcess proc) : base()
        {
            mBackendProcess = proc;
            mFSM = new StateMachine(this);
            proc.ProcessExitedEvent += ProcessExitedEvent;
            //cannot have identical transitions
            mFSM.addStateTransition(new StateMachine.StateTransition(ProcessState.Closed, StateEngine.Command.Connect),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Connected, null, () => { return mConnection.Connected; }));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Closed, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, mConnection.CleanUpSocket, null));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Connected, StateEngine.Command.LoadModel),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Loading, null, () => { return mConnection.Connected; }));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Connected, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, mConnection.CleanUpSocket, null));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Loading, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, mConnection.CleanUpSocket, null));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Loading, StateEngine.Command.ExecuteFinished),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Idle, DispatchOnCommandExecutedEvent));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Idle, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, mConnection.CleanUpSocket, null));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Idle, StateEngine.Command.Execute),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Busy));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Idle, StateEngine.Command.LoadModel),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Loading));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.Execute),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Busy));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.ExecuteFinished),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Idle, DispatchOnCommandExecutedEvent));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, mConnection.CleanUpSocket));
        }

        void ProcessExitedEvent(object source, RTextBackendProcess.ProcessExitedEventArgs e)
        {
            //kill any ongoing commands
            if (IsBusy() && mReceivingThreadCancellationSource != null)
            {
                mReceivingThreadCancellationSource.Cancel();
            }
            if (mFSM.CurrentState != ProcessState.Closed)
            {
                mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
            //reset invocation id counter since the backend process died or exited after an idle timeout
            mInvocationId = 0;
        }

        /**
         * \brief   Executes the fsm transition action.
         *
         * \param   nextState   State after the transition.
         */
        public void OnFsmTransition(ProcessState nextState)
        {
            //notify connectors that their backend in no longer available!
            if (OnStateChanged != null)
            {
                if(nextState != ProcessState.Busy && nextState != ProcessState.Loading)
                {
                    OnStateChanged(this, new StateChangedEventArgs(nextState, mBackendProcess.ProcKey, String.Empty)); 
                }
                else
                {
                    OnStateChanged(this, new StateChangedEventArgs(nextState, mBackendProcess.ProcKey, mActiveCommand));
                }
                
            }
        }

        /**
         * \property    public ProcessState ConnectorState
         *
         * \brief   Gets the state of the connector.
         *
         * \return  The connector state.
         */
        public ProcessState ConnectorState { get { return mFSM.CurrentState; } }

        public async Task<IResponseBase> ExecuteAsync<Command>(Command command, int timeout) where Command : RequestBase
        {
            if (command == null)
            {
                throw new InvalidOperationException("Command argument cannot be null.");
            }
            if (IsExecutionAllowed())
            {
                return await SendAsync<Command>(command, timeout, StateEngine.Command.Execute);
            }
            return null;
        }


        /**
         * \brief   Executes the given command synchronously.
         *
         * \tparam  Command Type of the command.
         * \param   command                 The command.
         * \param   [in,out]  invocationId    Identifier for the invocation.
         * \param   timeout                 (Optional) the timeout.
         *
         * \return  A Response type instance if the command could be executed succesfully and within the
         *          provided timeout, else null.
         *
         * \note    Users of this function must be prepared to receive null, as indication that
         *          something went wrong.
         */
        public void BeginExecute<Command>(Command command) where Command : RequestBase
        {
            if (command == null)
            {
                throw new InvalidOperationException("Command argument cannot be null.");
            }
            if (IsExecutionAllowed())
            {
                BeginSend<Command>(command, command.command == Constants.Commands.LOAD_MODEL ? StateEngine.Command.LoadModel : StateEngine.Command.Execute);
            }
        }

        /**
         *
         * \brief   Executes after a socket connection is made to autoload the model.
         */
        public void LoadModel()
        {           
            BeginExecute(LOAD_COMMAND);
        }

        /**
         * \brief   Begins an async send. Just send a command without caring about the response of the backend.
         *          Callers of this function should subscribe to CommandExecuted event to be able to receive a response.
         *          Usually used for long running commands like load_model.
         *
         * \tparam  Command Type of the command.
         * \param   command The command.
         * \param   cmd     The state machine command pointing to the next state.
         */
        private void BeginSend<Command>(Command command, StateEngine.Command cmd) where Command : RequestBase
        {
            bool aHasErrorOccured = false;
            try
            {
                command.invocation_id = mInvocationId++;
                mActiveCommand        = command.command;
                mFSM.MoveNext(cmd);                

                byte[] msg = null;
                using (var output = new StringWriter())
                {
                    JSON.Serialize<Command>(command, output, Options.IncludeInherited);
                    msg = PrepareRequestString(output);
                }

                if (mConnection.SendRequest(msg) != msg.Length)
                {
                    Logging.Logger.Instance.Append( Logging.Logger.MessageType.Error,
                                                    mBackendProcess.Workspace,
                                                    "void BeginSend<Command>(ref Command command, ref int invocationId) - Could not send request {0 }. Timeout of {1} has expired.", command.command, Constants.SEND_TIMEOUT);
                    aHasErrorOccured = true;
                }
            }
            catch (ArgumentNullException ex)
            {
                aHasErrorOccured = true;
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "void BeginSend<Command>(ref Command command, ref int invocationId) - ArgumentNullException : {0}", ex.ToString());
            }
            catch (SocketException ex)
            {
                aHasErrorOccured = true;
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "void BeginSend<Command>(ref Command command, ref int invocationId) - SocketException : {0}", ex.ToString());
            }
            catch (Exception ex)
            {
                aHasErrorOccured = true;
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "void BeginSend<Command>(ref Command command, ref int invocationId) - Exception : {0}", ex.ToString());

            }
            if (aHasErrorOccured)
            {
                mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
        }
       
        #endregion

        #region EventHandlers
        
        #endregion

        #region Helpers

        private async Task<IResponseBase> SendAsync<Command>(Command command, int timeout, StateEngine.Command cmd) where Command : RequestBase
        {
            command.invocation_id = mInvocationId++;
            try
            {
                mActiveCommand = command.command;
                mFSM.MoveNext(cmd);
                byte[] msg = null;

                using (var output = new StringWriter())
                {
                    JSON.Serialize<Command>(command, output, Options.IncludeInherited);
                    msg = PrepareRequestString(output);
                }

                // Send the data through the socket.

                //wait for manual reset event which indicates that the response has arrived
                mReceivedResponseEvent.Reset();
                if (mConnection.SendRequest(msg) != msg.Length)
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error,
                                                    mBackendProcess.Workspace,
                                                    "Could not send request to RTextService."
                                                  );
                    mFSM.MoveNext(StateEngine.Command.Disconnected);
                    return null;
                }

                mReceivingThreadCancellationSource = new CancellationTokenSource();

                Task<IResponseBase> errorTask = new Task<IResponseBase>(new Func<IResponseBase>(() =>
                {
                    while (true)
                    {
                        //wait 0.05 seconds
                        Thread.Sleep(50);
                        // Check is task should be ended!
                        if (mReceivingThreadCancellationSource.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    return null;
                }), mReceivingThreadCancellationSource.Token);

                Task<IResponseBase> receiverTask = new Task<IResponseBase>(new Func<IResponseBase>(() =>
                {
                    if (!mReceivedResponseEvent.WaitOne(timeout))
                    {
                        mFSM.MoveNext(StateEngine.Command.Disconnected);
                        mLastResponse = null;
                    }
                    mReceivingThreadCancellationSource.Cancel();
                    return mLastResponse;
                }));
                receiverTask.Start();
                errorTask.Start();

                var result = await Task.WhenAny<IResponseBase>(receiverTask, errorTask);
                return result.Result;
            }
            catch (ArgumentNullException ex)
            {
                mFSM.MoveNext(StateEngine.Command.Disconnected);
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "void send<Command>(ref Command command, ref int invocationId, int timeout) - Exception : {0}", ex.Message);
                return null;
            }
        }

        /**
         *
         * \brief   Tries to connect to a remote end point.
         *
         */
        private void Connect()
        {
            mConnection.CleanUpSocket();
            
            try
            {
                if (!mConnection.Connected)
                {
                    //will throw if something is wrong
                    IAsyncResult aConnectedResult = mConnection.BeginConnect("localhost", mBackendProcess.Port, ConnectCallback);
                    if (!aConnectedResult.AsyncWaitHandle.WaitOne(Constants.CONNECT_TIMEOUT))
                    {
                        throw new Exception("Connection to RText Service timed out!");
                    }
                    mFSM.MoveNext(StateEngine.Command.Connect);
                }
                //start receiving
                mConnection.BeginReceive(new AsyncCallback(ReceiveCallback));
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "void Connect() - Exception : {0}", ex.ToString());
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
                mConnection.EndConnect(ar);
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "void ConnectCallback(IAsyncResult ar) - Exception : {0}", ex.ToString());
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
                mConnection.EndReceive(ar);
                if (mConnection.BytesToRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    mConnection.Append();
                    // converts string into json objects
                    TryDeserialize();
                    mConnection.BeginReceive(new AsyncCallback(ReceiveCallback));
                }
            }
            catch (Exception ex)
            {
                mFSM.MoveNext(StateEngine.Command.Disconnected);
                Logging.Logger.Instance.Append( Logging.Logger.MessageType.Error,
                                                mBackendProcess.Workspace,
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
            return mFSM.CurrentState != ProcessState.Idle;
        }

        /**
        *
        * \brief   Recursively deserialize JSON messages.        
        * 
        */
        private void TryDeserialize()
        {
            if (mConnection.LengthMatched)
            {
                //we know the length but the reveived stream is not enough
                if (mConnection.RequiredLength <= mConnection.ReceivedMessage.Length)
                {
                    OnSufficientResponseLengthAcquired();                   
                }
            }
            else
            {
                Match aMatch = mMessageLengthRegex.Match(mConnection.ReceivedMessage.ToString());
                if (aMatch.Success)
                {
                    mConnection.LengthMatched   = true;
                    mConnection.JSONLength      = Int32.Parse(aMatch.Groups[1].Value);
                    mConnection.RequiredLength  = mConnection.JSONLength.ToString().Length + mConnection.JSONLength;
                    mConnection.ReceivedMessage = new StringBuilder(mConnection.ReceivedMessage.ToString(), mConnection.RequiredLength);
                    if (mConnection.RequiredLength <= mConnection.ReceivedMessage.Length)
                    {
                        OnSufficientResponseLengthAcquired();
                    }
                }
            }
        }

        private void OnSufficientResponseLengthAcquired()
        {
            mConnection.LengthMatched   = false;
            string aJSONmessage            = mConnection.ReceivedMessage.ToString(mConnection.JSONLength.ToString().Length, mConnection.JSONLength);
            mConnection.ReceivedMessage = new StringBuilder(mConnection.ReceivedMessage.ToString(mConnection.RequiredLength,
                                                               mConnection.ReceivedMessage.Length - mConnection.RequiredLength));
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
            using (var input = new StringReader(response))
            {
                switch (mActiveCommand)
                {
                    case Constants.Commands.LOAD_MODEL:
                        mLastResponse       = JSON.Deserialize<LoadResponse>(input, Options.IncludeInherited) as IResponseBase;
                        aIsResponceReceived = IsNotResponseOrErrorMessage();
                        break;
                    case Constants.Commands.LINK_TARGETS:
                        mLastResponse = JSON.Deserialize<LinkTargetsResponse>(input, Options.IncludeInherited) as IResponseBase;
                        aIsResponceReceived = IsNotResponseOrErrorMessage();
                        break;
                    case Constants.Commands.FIND_ELEMENTS:
                        mLastResponse = JSON.Deserialize<FindRTextElementsResponse>(input, Options.IncludeInherited) as IResponseBase;
                        aIsResponceReceived = IsNotResponseOrErrorMessage();
                        break;
                    case Constants.Commands.CONTENT_COMPLETION:
                        mLastResponse = JSON.Deserialize<AutoCompleteResponse>(input, Options.IncludeInherited) as IResponseBase;
                        aIsResponceReceived = IsNotResponseOrErrorMessage();
                        break;
                    case Constants.Commands.CONTEXT_INFO:
                        mLastResponse = JSON.Deserialize<ContextInfoResponse>(input, Options.IncludeInherited) as IResponseBase;
                        aIsResponceReceived = IsNotResponseOrErrorMessage();
                        break;
                }
            }
            if (aIsResponceReceived)
            {
                if (mInvocationId - 1 != mLastResponse.invocation_id)
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error,
                                                    mBackendProcess.Workspace,
                                                    "void AnalyzeResponse(ref string response, ref StateObject state) - Invocation id mismacth : Expected {0} - Received {1}",
                                                    mInvocationId - 1,
                                                    mLastResponse.invocation_id);
                    mLastResponse = null;
                }
                mLastInvocationId = mLastResponse.invocation_id;
                mFSM.MoveNext(StateEngine.Command.ExecuteFinished);
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
            switch (mLastResponse.type)
            {
                case Constants.Commands.PROGRESS:
                    if (OnProgressUpdated != null)
                    {
                        OnProgressUpdated(this, new ProgressResponseEventArgs
                        {
                            Response  = (ProgressResponse)mLastResponse,
                            Command   = mActiveCommand,
                            Workspace = mBackendProcess.ProcKey
                        });
                    }
                    mReceivedResponseEvent.Reset();
                    return false;
                case Constants.Commands.ERROR:
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "bool UpdateProgress( IResponseBase response ) - Backend reports unknown command error.");
                    return false;
                default:
                    return true;
            }
        }

        /**
         *
         * \brief   Prepare request string by appending the length of the message.
         *
         *
         * \param   stream  The stream.
         * \param   addLengthToStart Optional parameter. When set to true it will add the length of the JSON stream to the start of the string.                 
         *
         * \return  Response from backend as a string.
         */
        private byte[] PrepareRequestString( StringWriter swriter )
        {
            swriter.Flush();
            StringBuilder aExtendedString = new StringBuilder(swriter.GetStringBuilder().Length + 10);
            aExtendedString.Append(swriter.GetStringBuilder().Length);
            aExtendedString.Append(swriter.GetStringBuilder());
            return Encoding.ASCII.GetBytes(aExtendedString.ToString());
        }

        /**
         *
         * \brief   Dispatch on command executed event to notify all subscribers that the current command was executed.
         *
         */
        private void DispatchOnCommandExecutedEvent()
        {
            mReceivedResponseEvent.Set();
            //notify asynchronous subscribers that the response is received
            if (OnCommandExecuted != null)
            {
                OnCommandExecuted(this, new CommandCompletedEventArgs(mLastResponse, mLastInvocationId, mActiveCommand));
            }
        }

        /**
         * \brief   Query if this command execution is allowed.
         *
         * \return  true if execution is allowed, false if not.
         */
        private bool IsExecutionAllowed()
        {
            if (mBackendProcess.HasExited)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "RTextService is not running. Trying to restart service...");
                mBackendProcess.StartRTextService();
            }
            switch (mFSM.CurrentState)
            {
                case ProcessState.Closed:
                    Connect();
                    if (mFSM.CurrentState == ProcessState.Connected)
                    {
                        BeginSend(LOAD_COMMAND, StateEngine.Command.LoadModel);
                    }
                    break;
                case ProcessState.Connected:
                    BeginSend(LOAD_COMMAND, StateEngine.Command.LoadModel);
                    break;
                case ProcessState.Idle:
                    return true;
                case ProcessState.Loading:
                case ProcessState.Busy:
                default:
                    Trace.WriteLine(String.Format("Could not execute command! State : {0}", mFSM.CurrentState));
                    break;
            }
            return false;
        }

        #endregion
    }
}
