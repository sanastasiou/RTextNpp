using RTextNppPlugin.Automate.Protocol;
using RTextNppPlugin.Automate.StateEngine;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using RTextNppPlugin.Utilities;


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
        #region Fields
        private BlockingCollection<ProgressResponseStruct> mProgressQueue  = new BlockingCollection<ProgressResponseStruct>();   //!< Queue of progress
        private IPHostEntry mIpHostInfo                                    = Dns.GetHostEntry("localhost")                   ;   //!< Information describing the IP host
        private ManualResetEvent mReceivedResponseEvent                    = new ManualResetEvent(false)                     ;   //!< The received response event 
        private int mInvocationId                                          = 0                                               ;   //!< Identifier for the invocation
        private StateObject mReceiveStatus                                 = new StateObject()                               ;   //!< The receive status, used to store a state between calls of the receive callback
        private Regex mMessageLengthRegex                                  = new Regex(@"^(\d+)\{", RegexOptions.Compiled)   ;   //!< The message length regular expression
        private StateMachine mFSM                                                                                            ;   //!< The fsm
        private string mActiveCommand                                                                                        ;   //!< Indicates the currently executing command
        private RTextBackendProcess mBackendProcess                                                                          ;   //!< Indicates the backend process
        private CancellationTokenSource mReceivingThreadCancellationSource = null                                            ;   //!< Used to cancel a synchronous receiving thread without waiting for the timeout to complete.
        private IResponseBase mLastResponse                                = null                                            ;   //!< Holds the last response from the backend.
        private int mLastInvocationId                                      = -1                                              ;   //!< Holds the last invocation id from the backend.
        private Object mLock                                               = new Object()                                    ;   //!< Lock use to synchronize parallel command requests.
        #endregion

        #region Interface

        /**
         * \struct  ProgressResponseStruct
         *
         * \brief   Progress response structure. Maps a command with a progress response message.
         *
         */
        public struct ProgressResponseStruct
        {          
            public ProgressResponse Response;
            public String Command;
        }

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

            public StateChangedEventArgs(ProcessState state, string workspace)
            {
                State = state;
                Workspace = workspace;
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
         *
         * \brief   Constructor.
         *
         *
         * \param   pInfo       The information.
         * \param   filePath    Full pathname of the file associated with this connector.
         */
        public Connector( RTextBackendProcess proc) : base()
        {
            mBackendProcess = proc;
            mFSM = new StateMachine(this);
            proc.ProcessExitedEvent += ProcessExitedEvent;
            //cannot have identical transitions
            mFSM.addStateTransition(new StateMachine.StateTransition(ProcessState.Closed, StateEngine.Command.Connect),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Connected, null, () => { return mReceiveStatus.Socket.Connected; }));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Closed, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, CleanUpSocket, null));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Connected, StateEngine.Command.Execute),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Busy, null, () => { return mReceiveStatus.Socket.Connected; }));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Connected, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, CleanUpSocket, null));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.Execute),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Busy));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.ExecuteFinished),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Connected, DispatchOnCommandExecutedEvent));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, CleanUpSocket));
            
            //start consuming progress messages
            //mProgressManager = new StatusBarManager(ref this.mProgressQueue, this);
        }

        void ProcessExitedEvent(object source, RTextBackendProcess.ProcessExitedEventArgs e)
        {
            //kill any ongoing commands
            if (this.isBusy() && mReceivingThreadCancellationSource != null)
            {
                mReceivingThreadCancellationSource.Cancel();
            }
            if (this.mFSM.CurrentState != ProcessState.Closed)
            {
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
            //reset invocation id counter since the backend process died or exited after an idle timeout
            this.mInvocationId = 0;
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
                OnStateChanged(this, new StateChangedEventArgs(nextState, mBackendProcess.ProcKey));
            }
        }

        /**
         * \brief   Gets current state.
         *
         * \return  The current state.
         */
        public ProcessState getCurrentState()
        {
            return mFSM.CurrentState;
        }


        /**
         * \property    public ProcessState ConnectorState
         *
         * \brief   Gets the state of the connector.
         *
         * \return  The connector state.
         */
        public ProcessState ConnectorState { get { return this.mFSM.CurrentState; } }

        /**
         *
         * \brief   Executes the given command synchronously.
         *
         *
         * \tparam  Command      Type of the command.
         * \param   ref invocationid The current invocation id.
         * \tparam  Response         Type of the response.
         * \param   command          The command.
         *
         * \return  A Response type instance if the command could be executed succesfully and within the provided timeout, else null.
         * \remarks Users of this function must be prepared to receive null, as indication that something went wrong.
         */
        public IResponseBase execute<Command>( Command command, ref int invocationId, int timeout = -1 ) where Command : RequestBase                                                                                  
        {
            //sanity check
            if (command == null) return null;
            lock (mLock)
            {
                switch (this.mFSM.CurrentState)
                {
                    case ProcessState.Closed:
                        this.Connect();
                        if (this.mFSM.CurrentState == ProcessState.Connected)
                        {
                            //this.mProgressManager.setText("Connection with RText Service established!");
                            this.LoadModel(ref invocationId);
                        }
                        return null;
                    case ProcessState.Connected:
                        {
                            if (this.mFSM.GetNext(StateEngine.Command.Execute) == ProcessState.Busy)
                            {
                                //mProgressManager.setText(command.command);
                                if (timeout != -1)
                                {
                                    return this.send<Command>(ref command, ref invocationId, timeout);
                                }
                                else
                                {
                                    this.BeginSend<Command>(ref command, ref invocationId);
                                    return null;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Could not execute command!");
                                return null;
                            }
                        }
                    default:
                        //can't happen
                        return null;
                }
            }
        }

        public bool IsLoading
        {
            get
            {
                return (mActiveCommand == Constants.Commands.LOAD_MODEL);
            }
        }

        /**
         *
         * \brief   Executes after a socket connection is made to autoload the model.
         *
         *
         */
        private void LoadModel( ref int invocationId )
        {
            if(this.mBackendProcess.HasExited)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "ERROR: RTextService is not running. Trying to restart service...");
                mBackendProcess.StartRTextService();
                return;
            }
            //send asynchronous command since load model may take a long time to complete depending on the workspace
            RequestBase aTempCommand = new RequestBase { command = Constants.Commands.LOAD_MODEL, type = "request" };
            this.BeginSend<RequestBase>(ref aTempCommand, ref invocationId);
            OnFsmTransition(ProcessState.Loading);
        }

        /**
         *
         * \brief   Query if this object is busy.
         *
         *
         * \return  true if busy, false if not.
         */
        private bool isBusy()
        {
            return this.mFSM.CurrentState == ProcessState.Busy;
        }

        /**
         *
         * \brief   Begins an async send.
         *
         *
         * \tparam  ref Command Type of the command.
         * \param   ref invocationId The current invocation id.
         * \param   command The command.
         */
        private void BeginSend<Command>(ref Command command, ref int invocationId) where Command : RequestBase
        {
            try
            {
                invocationId = command.invocation_id = this.mInvocationId++;
                this.mFSM.MoveNext(StateEngine.Command.Execute);
                mActiveCommand = command.command;
                MemoryStream aStream = new MemoryStream();
                //if (mActiveCommand == Constants.Commands.LOAD_MODEL && mErrorListManager.Workspaces.Contains(ProcessInfo.ProcKey))
                //{
                //    DataContractJsonSerializer aDummySerializer = SerializerFactory<LoadResponse>.getSerializer();
                //    IResponseBase aDummyResponse = new LoadResponse { problems = new List<Problem>(), total_problems = 0, type = "response" };
                //    aDummySerializer.WriteObject(aStream, aDummyResponse);
                //    this.OnCommandExecuted(this, new CommandCompletedEventArgs(aDummyResponse, invocationId, mActiveCommand));
                //}
                aStream = new MemoryStream();
                DataContractJsonSerializer aSerializer = SerializerFactory<Command>.getSerializer();
                aSerializer.WriteObject(aStream, command);
                byte[] msg = Encoding.ASCII.GetBytes(this.PrepareRequestString(ref aStream) );
                // Send the data through the socket.
                int bytesSent;
                if (!Utilities.ProcessUtilities.TryExecute(SendRequest, Constants.SEND_TIMEOUT, msg, out bytesSent) || (bytesSent != msg.Length))
                {
                    Logging.Logger.Instance.Append( Logging.Logger.MessageType.Error,
                                                    mBackendProcess.Workspace,
                                                    "ERROR: void BeginSend<Command>(ref Command command, ref int invocationId) - Could not send request {0 }. Timeout of {1} has expired.", command.command, Constants.SEND_TIMEOUT);
                    this.mFSM.MoveNext(StateEngine.Command.Disconnected);
                    return;
                }
            }
            catch (ArgumentNullException ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "ERROR: void BeginSend<Command>(ref Command command, ref int invocationId) - ArgumentNullException : {0}", ex.ToString());
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
            catch (SocketException ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "ERROR: void BeginSend<Command>(ref Command command, ref int invocationId) - SocketException : {0}", ex.ToString());
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, mBackendProcess.Workspace, "ERROR: void BeginSend<Command>(ref Command command, ref int invocationId) - Exception : {0}", ex.ToString());
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
        }

        /**
         *
         * \brief   Send a synchronous command and waits for a response.
         *
         *
         * \tparam  Command Type of the command.
         * \param   ref command The command.
         * \param   ref invocationId The current invocation id.
         * \param   timeout The timeout.
         *
         * \return  The response.
         */
        private IResponseBase send<Command>(ref Command command, ref int invocationId, int timeout) where Command : RequestBase                                                                              
        {
            string aExceptionMessage = null;
            invocationId = command.invocation_id = this.mInvocationId++;
            try
            {
                this.mFSM.MoveNext(StateEngine.Command.Execute);
                mActiveCommand = command.command;
                MemoryStream aStream = new MemoryStream();
                DataContractJsonSerializer aSerializer = SerializerFactory<Command>.getSerializer();
                aSerializer.WriteObject(aStream, command);
                byte[] msg = Encoding.ASCII.GetBytes(this.PrepareRequestString(ref aStream));
                // Send the data through the socket.
                int bytesSent;
                //wait for manual reset event which indicates that the response has arrived
                this.mReceivedResponseEvent.Reset();
                if (!Utilities.ProcessUtilities.TryExecute(SendRequest, timeout, msg, out bytesSent) || (bytesSent != msg.Length))
                {
                    //StatusBarManager.writeToOutputWindow(String.Format("Could not send request to RTextService. Connector : {0}", ProcessInfo.RTextFilePath),
                    //                                        Utilities.HashUtilities.getGUIDfromString(ProcessInfo.ProcKey),
                    //                                        ProcessInfo.ProcKey);
                    //this.mFSM.MoveNext(StateEngine.Command.Disconnected);
                    return null;
                }
                //value was not received during specified timeout
                mReceivingThreadCancellationSource = new CancellationTokenSource();

                //dummy task, exits only if backend process exits!
                System.Threading.Tasks.TaskScheduler aScheduler = new Utilities.ThreadPerTaskScheduler();
                System.Threading.Tasks.Task aProcessExitWatcherTask = new System.Threading.Tasks.Task(() =>
                {
                    while (true)
                    {
                        //wait 0.1 seconds
                        Thread.Sleep(100);
                        // Check is task should be ended!
                        if (mReceivingThreadCancellationSource == null || mReceivingThreadCancellationSource.IsCancellationRequested || this.mFSM.CurrentState == ProcessState.Closed)
                        {
                            break;
                        }
                    }
                }, mReceivingThreadCancellationSource.Token);
                System.Threading.Tasks.Task aReceiverTask = new System.Threading.Tasks.Task(() =>
                {
                    if (!this.mReceivedResponseEvent.WaitOne(timeout))
                    {
                        this.mFSM.MoveNext(StateEngine.Command.Disconnected);
                    }
                    else
                    {
                        if (this.mReceiveStatus.Response != null)
                        {
                            this.mReceiveStatus.Response = null;
                        }
                    }
                });
                aProcessExitWatcherTask.Start(aScheduler);
                aReceiverTask.Start(aScheduler);
                int aTaskIndex = System.Threading.Tasks.Task.WaitAny(aProcessExitWatcherTask, aReceiverTask);
                switch (aTaskIndex)
                {
                    case 0:
                        //backend process exited, kill receiving task!
                        this.mReceivedResponseEvent.Set();
                        //indicate no valid backend response
                        mLastResponse                = null;
                        this.mReceiveStatus.Response = null;
                        //move to disconnected state!
                        this.mFSM.MoveNext(StateEngine.Command.Disconnected);
                        break;
                    case 1:
                        //ok receiving thread finished first kill watcher task
                        mReceivingThreadCancellationSource.Cancel();
                        break;
                }
                mReceivingThreadCancellationSource = null;
            }
            catch (ArgumentNullException ex)
            {
                aExceptionMessage = ex.Message;
            }
            catch (SocketException ex)
            {
                aExceptionMessage = ex.Message;
            }
            catch (Exception ex)
            {
                aExceptionMessage = ex.Message;
            }
            finally
            {
                if (aExceptionMessage != null)
                {
                    //StatusBarManager.writeToOutputWindow(   String.Format("Exception : {0}. Connector : {1}", aExceptionMessage, ProcessInfo.RTextFilePath),
                    //                                        Utilities.HashUtilities.getGUIDfromString(ProcessInfo.ProcKey),
                    //                                        ProcessInfo.ProcKey);
                    this.mFSM.MoveNext(StateEngine.Command.Disconnected);
                }
            }
            return mLastResponse;
        }

        #endregion

        #region EventHandlers
        
        #endregion

        #region Helpers

        /**
         *
         * \brief   Tries to connect to a remote end point.
         *
         *
         *
         * \return  True if connection is successful, false otherwise.
         */
        private void Connect()
        {
            //clean up any previous socket
            if (this.mReceiveStatus.Socket != null)
            {
                this.CleanUpSocket();
            }
            //if (!this.mConnectorManagerInstance.isProcessRunning(mBackendProcess.ProcessInfo.ProcKey))
            //{
                //StatusBarManager.writeToOutputWindow(   String.Format("ERROR : RText service for workspace {0} is not running. Trying to restart service...",
                //                                        this.mBackendProcess.ProcessInfo.RTextFilePath),
                //                                        Utilities.HashUtilities.getGUIDfromString(ProcessInfo.ProcKey),
                //                                        ProcessInfo.ProcKey
                //                                    );
                //mProgressManager.setText("Ready");
                //mProgressManager.stopAnimation();
                //ConnectorManager.getInstance.StartProcess(mBackendProcess.ProcessInfo);
                //return;
            //}
            this.mReceiveStatus.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                if (!this.mReceiveStatus.Socket.Connected)
                {
                    //will throw if something is wrong
                    IAsyncResult aConnectedResult = this.mReceiveStatus.Socket.BeginConnect("localhost", this.mBackendProcess.Port, ConnectCallback, this.mReceiveStatus);
                    if (!aConnectedResult.AsyncWaitHandle.WaitOne(Constants.CONNECT_TIMEOUT))
                    {
                        throw new Exception("Connection to RText Service timed out!");
                    }
                    this.mFSM.MoveNext(StateEngine.Command.Connect);
                }
                //start receiving
                this.mReceiveStatus.Socket.BeginReceive(this.mReceiveStatus.Buffer, 0, this.mReceiveStatus.BufferSize, 0, new AsyncCallback(receiveCallback), this.mReceiveStatus);
            }
            catch (Exception ex)
            {
            }
            finally
            {
            }
        }

        /**
        *
        * \brief   Tries to connect to a remote end point. If it succeeds signals main thread and sets a flag that the connection is successful.
        *
        *
        * \param ar THe async result
        * 
        * \return  True if connection is successful, false otherwise.
        */
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the connection.
                this.mReceiveStatus.Socket.EndConnect(ar);
            }
            catch (Exception ex)
            {
            }
        }

        /**
         *
         * \brief   Callback for asynchronous receive functionality.
         *
         *
         * \param   ar  The async result.
         */
        private void receiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                // Read data from the remote device.
                int aBytesRead = state.Socket.EndReceive(ar);
                if (aBytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.ReceivedMessage.Append(Encoding.ASCII.GetString(state.Buffer, 0, aBytesRead));
                    // converts string into json objects
                    tryDeserialize(ref state);
                    state.Socket.BeginReceive(state.Buffer, 0, state.BufferSize, 0, new AsyncCallback(receiveCallback), state);
                }
            }
            catch (Exception ex)
            {
                //StatusBarManager.writeToOutputWindow(   String.Format("Connector {0}, could not receive response from RTextService.\nException : {1}\nRText service backend process was forcibly terminated.",
                //                                        mBackendProcess.ProcessInfo.RTextFilePath, ex.Message),
                //                                        mBackendProcess.ProcessInfo.Guid.Value, mBackendProcess.ProcessInfo.ProcKey);
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
        }

        /**
        *
        * \brief   Recursively deserialize JSON messages, and adds them to the message queue
        *
        *
        * \param state State object used in the async receive method
        * 
        * \return  True if connection is successful, false otherwise.
        */
        private void tryDeserialize(ref StateObject state)
        {
            if (state.LengthMatched)
            {
                //we know the length but the reveived stream is not enough
                if (state.RequiredLength <= state.ReceivedMessage.Length)
                {
                    state.LengthMatched = false;
                    string aJSONmessage = state.ReceivedMessage.ToString(state.JSONLength.ToString().Length, state.JSONLength);
                    state.ReceivedMessage.Remove(0, state.RequiredLength);
                    //handle various responses
                    analyzeResponse(ref aJSONmessage, ref state);
                    if (state.ReceivedMessage.Length > 0)
                    {
                        tryDeserialize(ref state);
                    }
                }
            }
            else
            {
                Match aMatch = mMessageLengthRegex.Match(state.ReceivedMessage.ToString());
                if (aMatch.Success)
                {
                    state.LengthMatched = true;
                    state.JSONLength = Int32.Parse(aMatch.Groups[1].Value);
                    state.RequiredLength = state.JSONLength.ToString().Length + state.JSONLength;
                    if (state.RequiredLength <= state.ReceivedMessage.Length)
                    {
                        state.LengthMatched = false;
                        string aJSONmessage = state.ReceivedMessage.ToString(state.JSONLength.ToString().Length, state.JSONLength);
                        state.ReceivedMessage.Remove(0, state.RequiredLength);
                        //handle various responses
                        analyzeResponse(ref aJSONmessage, ref state);
                        if (state.ReceivedMessage.Length > 0)
                        {
                            tryDeserialize(ref state);
                        }
                    }
                }
            }
        }

        /**
         *
         * \brief   Analyzes the last received response.
         *
         *
         * \param [in,out]  response    The response string.
         * \param [in,out]  state       The state object.
         */
        private void analyzeResponse(ref string response, ref StateObject state)
        {
            int aResponseInvocationId = -1;
            using (System.IO.MemoryStream aStream = new System.IO.MemoryStream(System.Text.Encoding.ASCII.GetBytes(response)))
            {
                switch (mActiveCommand)
                {
                    case Constants.Commands.LOAD_MODEL:
                        mLastResponse = SerializerFactory<LoadResponse>.getSerializer().ReadObject(aStream) as IResponseBase;
                        if (updateProgress(mLastResponse))
                        {
                            mLastResponse = mLastResponse as IResponseBase;
                            //mErrorListManager[mBackendProcess.ProcessInfo.ProcKey] = mLastResponse as LoadResponse;
                            aResponseInvocationId = mLastResponse.invocation_id;
                        }
                        else return;
                        break;
                    case Constants.Commands.LINK_TARGETS:
                        mLastResponse = SerializerFactory<LinkTargetsResponse>.getSerializer().ReadObject(aStream) as IResponseBase;
                        if (updateProgress(mLastResponse))
                        {
                            aResponseInvocationId = mLastResponse.invocation_id;
                        }
                        else return;
                        break;
                    case Constants.Commands.FIND_ELEMENTS:
                        mLastResponse = SerializerFactory<FindRTextElementsResponse>.getSerializer().ReadObject(aStream) as IResponseBase;
                        if (updateProgress(mLastResponse))
                        {
                            aResponseInvocationId = mLastResponse.invocation_id;
                        }
                        else return;
                        break;
                    case Constants.Commands.CONTENT_COMPLETION:
                        mLastResponse = SerializerFactory<AutoCompleteResponse>.getSerializer().ReadObject(aStream) as IResponseBase;
                        if (updateProgress(mLastResponse))
                        {
                            aResponseInvocationId = mLastResponse.invocation_id;
                        }
                        break;
                    case Constants.Commands.CONTEXT_INFO:
                        mLastResponse = SerializerFactory<ContextInfoResponse>.getSerializer().ReadObject(aStream) as IResponseBase;
                        if (updateProgress(mLastResponse))
                        {
                            aResponseInvocationId = mLastResponse.invocation_id;
                        }
                        break;
                }
            }
            
            state.Response        = String.Empty;
            state.ReceivedMessage = new StringBuilder(state.BufferSize);
            //mProgressManager.setText(String.Format("Ready"));
            if ( this.mInvocationId - 1 != aResponseInvocationId )
            {
                //StatusBarManager.writeToOutputWindow(   String.Format("Invocation id mismacth : Expected {0} - Received {1}", this.mInvocationId - 1, aResponseInvocationId),
                //                                        mBackendProcess.ProcessInfo.Guid.Value,
                //                                        mBackendProcess.ProcessInfo.ProcKey
                //                                    );
            }
            mLastInvocationId = aResponseInvocationId;
            this.mFSM.MoveNext(StateEngine.Command.ExecuteFinished);     
        }

        /**
         *
         * \brief   Updates the progress described by response.
         *
         *
         * \param [in,out]  response    The response.
         *
         * \return  true if this is not a progress message, false otherwise.
         */
        private bool updateProgress( IResponseBase response )
        {
            if (response.type == Constants.Commands.PROGRESS)
            {
                this.mProgressQueue.Add(new ProgressResponseStruct { Response = (ProgressResponse)response, Command = mActiveCommand });
                this.mReceivedResponseEvent.Reset();
                return false;
            }
            else if (response.type == Constants.Commands.ERROR)
            {
                //StatusBarManager.writeToOutputWindow("Backend reports unknown command error.", mBackendProcess.ProcessInfo.Guid.Value, mBackendProcess.ProcessInfo.ProcKey );
                return false;
            }
            return true;
        }

        /**
         *
         * \brief   Sends a request synchronously.
         *
         *
         * \param   request The request.
         *
         * \return  The bytes that were actually send.
         */
        private int SendRequest(byte[] request)
        {
            return this.mReceiveStatus.Socket.Send(request);
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
        private string PrepareRequestString( ref MemoryStream stream )
        {
            stream.Position = 0;
            StreamReader aReader = new StreamReader(stream);
            string aStringWithoutBytes =  aReader.ReadToEnd();
            return aStringWithoutBytes.Length + aStringWithoutBytes;
        }

        //!< State object for receiving data from remote device.
        public class StateObject
        {
            // Receive buffer.
            private byte[] mBuffer = new byte[Constants.BUFFER_SIZE];
            // Received data string.
            private StringBuilder mReceivedMessage = new StringBuilder( Constants.BUFFER_SIZE );
            // Response string
            private string mResponseString;

            public int BufferSize { get { return Constants.BUFFER_SIZE; } }
            public byte[] Buffer { get { return mBuffer; } }
            public StringBuilder ReceivedMessage { get { return mReceivedMessage; } set { mReceivedMessage = value; } }
            public string Response { get { return this.mResponseString; } set { this.mResponseString = value; } }
            public Socket Socket { get; set; }
            public bool LengthMatched { get; set; }
            public int RequiredLength { get; set; }
            public int JSONLength { get; set; }
        }

        /**
         *
         * \brief   Cleans up and disposes the socket.
         *
         */
        private void CleanUpSocket()
        {
            if (this.mReceiveStatus.Socket.Connected)
            {
                this.mReceiveStatus.Socket.Disconnect(false);
                this.mReceiveStatus.Socket.Shutdown(SocketShutdown.Both);
                this.mReceiveStatus.Socket.Close(Constants.CONNECT_TIMEOUT);
                this.mReceiveStatus.Socket.Dispose();
            }
        }

        /**
         *
         * \brief   Dispatch on command executed event to notify all subscribers that the current command was executed.
         *
         */
        private void DispatchOnCommandExecutedEvent()
        {
            //notify synchronous subscribers that the response is received
            mReceivedResponseEvent.Set();
            //notify asynchronous subscribers that the response is received
            if (this.OnCommandExecuted != null)
            {
                this.OnCommandExecuted(this, new CommandCompletedEventArgs(mLastResponse, mLastInvocationId, mActiveCommand));
            }
        }

        #endregion
    }
}
