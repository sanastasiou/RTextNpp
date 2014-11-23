using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ESRLabs.RTextEditor.Protocol;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using ESRLabs.RTextEditor.StateEngine;
using ESRLabs.RTextEditor.Utilities;
using System.Runtime.Serialization.Json;


namespace ESRLabs.RTextEditor
{
    /**
     * @class   Connector
     *
     * @brief   Connector. All commands are going through a connector instance.
     *
     * @author  Stefanos Anastasiou
     * @date    09.12.2012
     */
    public class Connector
    {
        #region Fields
        private BlockingCollection<ProgressResponseStruct> mProgressQueue  = new BlockingCollection<ProgressResponseStruct>();   //!< Queue of progress
        private IPHostEntry mIpHostInfo                                    = Dns.GetHostEntry("localhost")                   ;   //!< Information describing the IP host
        private ManualResetEvent mReceivedResponseEvent                    = new ManualResetEvent(false)                     ;   //!< The received response event 
        private int mInvocationId                                          = 0                                               ;   //!< Identifier for the invocation
        private StateObject mReceiveStatus                                 = new StateObject()                               ;   //!< The receive status, used to store a state between calls of the receive callback
        private Regex mMessageLengthRegex                                  = new Regex(@"^(\d+):\d+\{", RegexOptions.Compiled)   ;   //!< The message length regular expression
        private StateMachine mFSM                                                                                            ;   //!< The fsm
        private string mActiveCommand                                                                                        ;   //!< Indicates the currently executing command
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
         * \author  Stefanos Anastasiou
         * \date    01.02.2013
         */
        public struct ProgressResponseStruct
        {
            public Protocol.ProgressResponse Response;
            public String Command;
        }

        /**
         * @fn  public delegate void CommandExecuted(object source, CommandCompletedEventArgs e);
         *
         * @brief   Delegate for the CommandCompleted event.
         *
         * @author  Stefanos Anastasiou
         * @date    17.11.2012
         *
         * @param   source  Source object of the event.
         * @param   e       Command completed event information.
         */
        public delegate void CommandExecuted(object source, CommandCompletedEventArgs e);

        public event CommandExecuted CommmandExecuted;                                      //!< Event queue for all listeners interested in CommmandExecuted events.

        /**
         * @class   CommandCompletedEventArgs
         *
         * @brief   Command completed is an event which is fired to notify subscribers that an async command has finished executing.
         *
         * @author  Stefanos Anastasiou
         * @date    25.11.2012
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
             * \fn  public CommandCompletedEventArgs(string response, int invocationId )
             *
             * \brief   Constructor.
             *
             * \author  Stefanos Anastasiou
             * \date    24.05.2013
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
         * @fn  public Connector( ConnectorManager.ProcessInfo pInfo, string filePath)
         *
         * @brief   Constructor.
         *
         * @author  Stefanos Anastasiou
         * @date    17.11.2012
         *
         * @param   pInfo       The information.
         * @param   filePath    Full pathname of the file associated with this connector.
         */
        public Connector() : base()
        {
            mFSM = new StateMachine();
            //cannot have identical transitions
            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Closed, StateEngine.Command.Connect),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Connected, null, isConnected));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Closed, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, CleanUpSocket, null));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Connected, StateEngine.Command.Execute),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Busy, null, isConnected));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Connected, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, CleanUpSocket, null));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.Execute),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Busy));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.ExecuteFinished),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Connected, DispatchOnCommandExecutedEvent));

            mFSM.addStateTransition( new StateMachine.StateTransition(ProcessState.Busy, StateEngine.Command.Disconnected),
                                     new StateMachine.ProcessStateWithAction(ProcessState.Closed, CleanUpSocket));
        }

        /**
         * @property    public ProcessState ConnectorState
         *
         * @brief   Gets the state of the connector.
         *
         * @return  The connector state.
         */
        public ProcessState ConnectorState { get { return this.mFSM.CurrentState; } }

        /**
         * @fn  public IResponseBase execute<Command, Response>(RequestBase command) where Command : RequestBase where Response : ResponseBase
         *
         * @brief   Executes the given command synchronously.
         *
         * @author  Stefanos Anastasiou
         * @date    24.11.2012
         *
         * @tparam  Command      Type of the command.
         * @param   ref invocationid The current invocation id.
         * @tparam  Response         Type of the response.
         * @param   command          The command.
         *
         * @return  A Response type instance if the command could be executed succesfully and within the provided timeout, else null.
         * @remarks Users of this function must be prepared to receive null, as indication that something went wrong.
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
                        Trace.WriteLine("Connecting to RText Service...");
                        this.Connect();
                        if (this.mFSM.CurrentState == ProcessState.Connected)
                        {
                            Trace.WriteLine("Connection with RText Service established!");
                            this.LoadModel(ref invocationId);
                        }
                        return null;
                    case ProcessState.Connected:
                        {
                            if (this.mFSM.GetNext(StateEngine.Command.Execute) == ProcessState.Busy)
                            {
                                if (timeout != -1)
                                {
                                    return this.send<Command>(ref command, ref invocationId, timeout);
                                }
                                else
                                {
                                    this.beginSend<Command>(ref command, ref invocationId);
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

        /**
         * \fn  bool isConnected()
         *
         * \brief   Query if this connector is connected with the backend service.
         *
         * \author  Stefanos Anastasiou
         * \date    09.03.2013
         *
         * \return  true if connected, false if not.
         */
        bool isConnected()
        {
            return this.mReceiveStatus.Socket.Connected;
        }

        /**
         * @fn  private LoadModel()
         *
         * @brief   Executes after a socket connection is made to autoload the model.
         *
         * @author  Stefanos Anastasiou
         * @date    17.11.2012
         *
         */
        private void LoadModel( ref int invocationId )
        {

            //send asynchronous command since load model may take a long time to complete depending on the workspace
            //Protocol.RequestBase aTempCommand = new Protocol.RequestBase { command = Constants.Commands.LOAD_MODEL, type = "request" };
            
            //this.beginSend<Protocol.RequestBase>(ref aTempCommand, ref invocationId);
        }

        /**
         * @fn      private bool isBusy()
         *
         * @brief   Query if this object is busy.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @return  true if busy, false if not.
         */
        private bool isBusy()
        {
            return this.mFSM.CurrentState == ProcessState.Busy;
        }

      
        public void beginSend(string jsonString)
        {
            try
            {
                byte[] msg = Encoding.ASCII.GetBytes(jsonString);
                //byte[] msg = Encoding.ASCII.GetBytes(this.PrepareRequestString(ref aStream));
                // Send the data through the socket.
                int bytesSent;
                if (!Utilities.ProcessUtilities.TryExecute(SendRequest, Constants.SEND_TIMEOUT, msg, out bytesSent) || (bytesSent != msg.Length))
                {
                    this.mFSM.MoveNext(StateEngine.Command.Disconnected);
                    return;
                }
            }
            catch (ArgumentNullException ex)
            {
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
            catch (SocketException ex)
            {
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
            catch (Exception ex)
            {
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
        }


        /**
         * @fn  private void beginSend<Command>(RequestBase command) where Command : RequestBase
         *
         * @brief   Begins an async send.
         *
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         *
         * @tparam  ref Command Type of the command.
         * @param   ref invocationId The current invocation id.
         * @param   command The command.
         */
        private void beginSend<Command>(ref Command command, ref int invocationId) where Command : RequestBase
        {
            try
            {
                invocationId = command.invocation_id = this.mInvocationId++;
                this.mFSM.MoveNext(StateEngine.Command.Execute);
                mActiveCommand = command.command;
                MemoryStream aStream = new MemoryStream();
                if (mActiveCommand == Constants.Commands.LOAD_MODEL)
                {
                    DataContractJsonSerializer aDummySerializer = SerializerFactory<LoadResponse>.getSerializer();
                    IResponseBase aDummyResponse = new LoadResponse { problems = new List<Problem>(), total_problems = 0, type = "response" };
                    aDummySerializer.WriteObject(aStream, aDummyResponse);
                    this.CommmandExecuted(this, new CommandCompletedEventArgs(aDummyResponse, invocationId, mActiveCommand));
                }
                aStream = new MemoryStream();
                DataContractJsonSerializer aSerializer = SerializerFactory<Command>.getSerializer();
                aSerializer.WriteObject(aStream, command);
                byte[] msg = Encoding.ASCII.GetBytes(this.PrepareRequestString(ref aStream) );
                // Send the data through the socket.
                int bytesSent;
                if (!Utilities.ProcessUtilities.TryExecute(SendRequest, Constants.SEND_TIMEOUT, msg, out bytesSent) || (bytesSent != msg.Length))
                {
                    this.mFSM.MoveNext(StateEngine.Command.Disconnected);
                    return;
                }
            }
            catch (ArgumentNullException ex)
            {
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
            catch (SocketException ex)
            {
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
            catch (Exception ex)
            {
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
        }

        private void send(string json)
        {
            string aExceptionMessage = null;
            try
            {
                byte[] msg = Encoding.ASCII.GetBytes(json);
                // Send the data through the socket.
                int bytesSent;
                //wait for manual reset event which indicates that the response has arrived
                this.mReceivedResponseEvent.Reset();
                if (!Utilities.ProcessUtilities.TryExecute(SendRequest, 100000, msg, out bytesSent) || (bytesSent != msg.Length))
                {
                    this.mFSM.MoveNext(StateEngine.Command.Disconnected);
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
                    if (!this.mReceivedResponseEvent.WaitOne(10000))
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
                        mLastResponse = null;
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
        }

        /**
         * @fn  private IResponseBase send<Command>(RequestBase command, int timeout) where Command : RequestBase
         *
         * @brief   Send a synchronous command and waits for a response.
         *
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         *
         * @tparam  Command Type of the command.
         * @param   ref command The command.
         * @param   ref invocationId The current invocation id.
         * @param   timeout The timeout.
         *
         * @return  The response.
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
                    this.mFSM.MoveNext(StateEngine.Command.Disconnected);
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
         * @fn  private bool Connect( )
         *
         * @brief   Tries to connect to a remote end point.
         *
         * @author  Stefanos Anastasiou
         * @date    18.11.2012
         *
         *
         * @return  True if connection is successful, false otherwise.
         */
        public void Connect()
        {
            //clean up any previous socket
            if (this.mReceiveStatus.Socket != null)
            {
                this.CleanUpSocket();
            }
            this.mReceiveStatus.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                if (!this.mReceiveStatus.Socket.Connected)
                {
                    Trace.WriteLine("Connecting...");
                    //will throw if something is wrong
                    IAsyncResult aConnectedResult = this.mReceiveStatus.Socket.BeginConnect("localhost", 9001, ConnectCallback, this.mReceiveStatus);
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
                Trace.WriteLine(ex.Message);
            }
        }

        /**
        * @fn  private void ConnectCallback( IAsyncResult ar )
        *
        * @brief   Tries to connect to a remote end point. If it succeeds signals main thread and sets a flag that the connection is successful.
        *
        * @author  Stefanos Anastasiou
        * @date    18.11.2012
        *
        * @param ar THe async result
        * 
        * @return  True if connection is successful, false otherwise.
        */
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Complete the connection.
                this.mReceiveStatus.Socket.EndConnect(ar);
                this.PrintToConsole("Connected to RText Service!");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /**
         * @fn  private void receiveCallback(IAsyncResult ar)
         *
         * @brief   Callback for asynchronous receive functionality.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @param   ar  The async result.
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
                Trace.WriteLine(ex.Message);
                this.mFSM.MoveNext(StateEngine.Command.Disconnected);
            }
        }

        /**
        * @fn      private void tryDeserialize(StateObject state)
        *
        * @brief   Recursively deserialize JSON messages, and adds them to the message queue
        *
        * @author  Stefanos Anastasiou
        * @date    18.11.2012
        *
        * @param state State object used in the async receive method
        * 
        * @return  True if connection is successful, false otherwise.
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
         * @fn  private void analyzeResponse(ref string response, ref StateObject state)
         *
         * @brief   Analyzes the last received response.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @param [in,out]  response    The response string.
         * @param [in,out]  state       The state object.
         */
        private void analyzeResponse(ref string response, ref StateObject state)
        {
            int aResponseInvocationId = -1;
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            // Begin timing
            stopwatch.Start();
            using (System.IO.MemoryStream aStream = new System.IO.MemoryStream(System.Text.Encoding.ASCII.GetBytes(response)))
            {
                
            }
            
            state.Response        = String.Empty;
            state.ReceivedMessage = new StringBuilder(state.BufferSize);

            if ( this.mInvocationId - 1 != aResponseInvocationId )
            {

            }
            mLastInvocationId = aResponseInvocationId;
            this.mFSM.MoveNext(StateEngine.Command.ExecuteFinished);     
        }


        /**
         * @fn  private int SendRequest(byte[] request)
         *
         * @brief   Sends a request synchronously.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @param   request The request.
         *
         * @return  The bytes that were actually send.
         */
        private int SendRequest(byte[] request)
        {
            return this.mReceiveStatus.Socket.Send(request);
        }

        /**
         * @fn  private string PrepareRequestString( MemoryStream stream )
         *
         * @brief   Prepare request string by appending the length of the message.
         *
         * @author  Stefanos Anastasiou
         * @date    18.11.2012
         *
         * @param   stream  The stream.
         * @param   addLengthToStart Optional parameter. When set to true it will add the length of the JSON stream to the start of the string.                 
         *
         * @return  Response from backend as a string.
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
         * @fn  private void CleanUpSocket()
         *
         * @brief   Cleans up and disposes the socket.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
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
         * \fn  private void DispatchOnCommandExecutedEvent()
         *
         * \brief   Dispatch on command executed event to notify all subscribers that the current command was executed.
         *
         * \author  Stefanos Anastasiou
         * \date    30.05.2013
         */
        private void DispatchOnCommandExecutedEvent()
        {
            //notify synchronous subscribers that the response is received
            mReceivedResponseEvent.Set();
            //notify asynchronous subscribers that the response is received
            if (this.CommmandExecuted != null)
            {
                this.CommmandExecuted(this, new CommandCompletedEventArgs(mLastResponse, mLastInvocationId, mActiveCommand));
            }
        }

        /**
         * @fn  private void PrintToConsole(string message)
         *
         * @brief   Prints a message to console.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @param   message The message.
         */
        private void PrintToConsole(string message)
        {
            Trace.WriteLine(message);
        }

        #endregion
    }
}
