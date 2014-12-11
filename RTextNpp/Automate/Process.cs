using FileSystemWactherCLRWrapper;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RTextNppPlugin.RTextEditor
{
    /**
     * \class   Process
     *
     * \brief   Process wrapper class over the .NET process class. Fixes several bugs regarding IO redirect.
     *
     * \author  Stefanos Anastasiou
     * \date    31.05.2013
     */
    public class Process : IDisposable
    {
        #region Fields
        private System.Diagnostics.Process mProcess         = null;
        private Utilities.ProcessInfo mPInfo                = null;
        private FileSystemWatcher mFileSystemWatcher        = null;                                                                         //!< Observes all automate files for modifications.
        private FileSystemWatcher mWorkspaceSystemWatcher   = null;                                                                         //!< Observes .rtext file for any modifications.
        private CancellationTokenSource mStdOutTokenSource  = null;
        private CancellationToken mStdOutToken;
        private CancellationTokenSource mStdErrTokenSource  = null;
        private CancellationToken mStdErrToken;
        private Task mStdOutReaderTask                      = null;
        private Task mStdErrReaderTask                      = null;
        private bool mIsProcessStarting                     = true;
        private int mPort                                   = -1;
        private Connector mConnector                        = null;
        private readonly Regex mBackendInitResponseRegex    = new Regex(@"^RText service, listening on port (\d+)$", RegexOptions.Compiled);
        private ManualResetEvent mTimeoutEvent              = new ManualResetEvent(false);
        private DispatcherTimer mTimer;
        private bool mIsMessageDisplayed                    = false;
        private IAsyncResult mGetPortNumberAsyncResult;
        private bool mIsPortNumberRetrieved                 = false;                                                                         //!< Whether a process has started and the port number is retrieved.
        private int mInvocationId                           = -1;                                                                            //!< The invocation id used when loading model.. maybe it can be used in the furure
        private string mExtension                           = String.Empty;                                                                  //!< The associated extension string.
        private string mAutoRunKey                          = String.Empty;                                                                  //!< The autorun registry value.

        /**
         * @fn  public delegate bool AsyncMethodCaller(int timeout, out int portNumber );
         *
         * @brief   Asynchronous method caller. Used to run the process start up asynchronously so that the UI thread doesn't get blocked.
         *
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         *
         * @param   timeout         The timeout.
         * @param [out] portNumber  The port number.
         *
         * @return  .
         */
        public delegate bool AsyncMethodCaller(int timeout, out int portNumber );
        AsyncMethodCaller mStartProcessDelegate;	                                                                                        //!< The start process delegate
        #endregion

        #region Interface
        
        #region Events
        /**
         * @fn  public delegate void ConnectorCreated(object source, ConnectorCreatedEventArgs e);
         *
         * @brief   Delegate to inform subscribers that a connector is created.
         *
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         *
         * @param   source  Source of the event.
         * @param   e       Connector created event information.
         */
        public delegate void ConnectorCreated(object source, ConnectorCreatedEventArgs e);
        public event ConnectorCreated ConnectorCreatedEvent;//!< Event queue for all listeners interested in ConnectorCreated events.

        /**
         * @class   ConnectorCreatedEventArgs
         *
         * @brief   Additional information for connector created events.
         *
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         */
        public class ConnectorCreatedEventArgs : EventArgs
        {
            public string ProcessKey { get; private set; }

            public ConnectorCreatedEventArgs(string procKey)
            {
                ProcessKey = procKey;
            }
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
        public delegate void ProcessExited(object source, ProcessExitedEventArgs e);

        public event ProcessExited ProcessExitedEvent;                                                                                      //!< Event queue for all listeners interested in ProcessExited events.

        /**
         * @class   CommandCompletedEventArgs
         *
         * @brief   Command completed is an event which is fired to notify subscribers that an async command has finished executing.
         *
         * @author  Stefanos Anastasiou
         * @date    25.11.2012
         */
        public class ProcessExitedEventArgs : EventArgs
        {
            public string ProcessKey { get; private set; }

            public ProcessExitedEventArgs(string procKey)
            {
                ProcessKey = procKey;
            }
        }
        #endregion
        
        /**
         * @fn  public Process(string rTextFilePath, string workingDirectory, string commandLine,
         *      string processKey)
         *
         * @brief   Constructor.
         *
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         *
         * @param   rTextFilePath       Full pathname of the .rtext file.
         * @param   workingDirectory    Pathname of the working directory.
         * @param   commandLine         The command line.
         * @param   processKey          The process key.
         */
        public Process(string rTextFilePath, string workingDirectory, string commandLine, string processKey, string extenstion )
        {
            mPInfo = new Utilities.ProcessInfo(workingDirectory, rTextFilePath, commandLine, null, processKey);
            mTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                //wait one second before loading model in case of thousands of events / sec
                Interval = TimeSpan.FromMilliseconds(50)
            };
            mTimer.Tick += OnTimerElapsed;
            mExtension = extenstion;
        }

        /**
         * @property    public Utilities.ProcessInfo ProcessInfo
         *
         * @brief   Gets the information describing the process.
         *
         * @return  Information describing the process.
         */
        public Utilities.ProcessInfo ProcessInfo
        {
            get
            {
                return this.mPInfo;
            }
        }

        /**
         * @fn  public bool StartRTextService()
         *
         * @brief   Tries to start the backend service, as it is specified in the .rtext file.
         *
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         *
         * @return  true if it succeeds, false if it fails.
         */
        public void StartRTextService()
        {
            if ( mGetPortNumberAsyncResult == null || mGetPortNumberAsyncResult.IsCompleted)
            {
                //process was never started
                Match aMatch = Regex.Match(mPInfo.CommandLine, @"(^\s*\S+)(.*)", RegexOptions.Compiled);
                System.Diagnostics.ProcessStartInfo aProcessStartInfo = new ProcessStartInfo(aMatch.Groups[1].Value, aMatch.Groups[2].Value);
                mPInfo.Name = aMatch.Groups[1].Value;
                aProcessStartInfo.CreateNoWindow = true;
                aProcessStartInfo.RedirectStandardError = true;
                aProcessStartInfo.RedirectStandardOutput = true;
                aProcessStartInfo.UseShellExecute = false;
                aProcessStartInfo.WorkingDirectory = mPInfo.WorkingDirectory;
                mProcess = new System.Diagnostics.Process();
                mProcess.StartInfo = aProcessStartInfo;
                //add filewacthers for .rtext file and all associated extensions
                string aExtensions = mPInfo.ProcKey.Substring(mPInfo.RTextFilePath.Length, mPInfo.ProcKey.Length - mPInfo.RTextFilePath.Length);
                Regex regexObj = new Regex(@"\.\w+");
                Match matchResults = regexObj.Match(aExtensions);
                string aExtensionsFilter = String.Empty;
                while (matchResults.Success)
                {
                    aExtensionsFilter += "*" + matchResults.Value + ";";
                    matchResults = matchResults.NextMatch();
                }
                aExtensionsFilter = aExtensionsFilter.Substring(0, aExtensionsFilter.Length - 1);
                mFileSystemWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher( System.IO.Path.GetDirectoryName(mPInfo.RTextFilePath), 
                                                                                        false,
                                                                                        aExtensionsFilter,
                                                                                        String.Empty,
                                                                                        (uint)(int)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime | System.IO.NotifyFilters.Size | System.IO.NotifyFilters.LastAccess | System.IO.NotifyFilters.Attributes),
                                                                                        true);
                mFileSystemWatcher.Changed += FileSystemWatcherChanged;
                mFileSystemWatcher.Deleted += FileSystemWatcherDeleted;
                mFileSystemWatcher.Created += FileSystemWatcherChanged;
                mFileSystemWatcher.Renamed += FileSystemWatcherRenamed;
                mFileSystemWatcher.Error   += ProcessError;
                //finally add .rtext wacther
                mWorkspaceSystemWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher( System.IO.Path.GetDirectoryName(mPInfo.RTextFilePath),
                                                                                             false,
                                                                                             "*" + Constants.WORKSPACE_TYPE,
                                                                                             String.Empty,
                                                                                             (uint)(int)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime | System.IO.NotifyFilters.Size | System.IO.NotifyFilters.LastAccess | System.IO.NotifyFilters.Attributes),
                                                                                             false
                                                                                           );
                mWorkspaceSystemWatcher.Changed += mWorkspaceSystemWatcher_Changed;
                mWorkspaceSystemWatcher.Deleted += mWorkspaceSystemWatcher_Deleted;
                mWorkspaceSystemWatcher.Created += mWorkspaceSystemWatcher_Created;
                mWorkspaceSystemWatcher.Renamed += mWorkspaceSystemWatcher_Renamed;             
                mWorkspaceSystemWatcher.Error   += ProcessError;

                mProcess.Exited += new EventHandler(OnProcessExited);
                //disable doskey or whatever actions are associated with cmd.exe
                mAutoRunKey = disabledCmdExeCustomization();
                mProcess.Start();

                mProcess.EnableRaisingEvents = true;
                //start reaading asynchronously with tasks
                mStdOutTokenSource = new CancellationTokenSource();
                mStdOutToken = mStdOutTokenSource.Token;
                mStdErrTokenSource = new CancellationTokenSource();
                mStdErrToken = mStdErrTokenSource.Token;
                mStdOutReaderTask = Task.Factory.StartNew(() => ReadStream(mProcess.StandardOutput, mStdOutToken), mStdOutToken);
                mStdErrReaderTask = Task.Factory.StartNew(() => ReadStream(mProcess.StandardError, mStdErrToken), mStdErrToken);
                mIsProcessStarting = true;
                mTimeoutEvent.Reset();

                mStartProcessDelegate = new AsyncMethodCaller(this.GetPortNumber);
                this.mPort = -1; //reinit port every time this function is called
                this.mGetPortNumberAsyncResult = mStartProcessDelegate.BeginInvoke( Constants.INITIAL_RESPONSE_TIMEOUT,
                                                                                    out this.mPort,
                                                                                    new AsyncCallback(PortNumberRetrievalCompleted),
                                                                                    mStartProcessDelegate);
            }
        }

        void mWorkspaceSystemWatcher_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            //.rtext should never be renamed - process must be restarted - which will result to an error
            RestartProcess();
        }

        void mWorkspaceSystemWatcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext should never be recreated - process must be restarted
            RestartProcess();
        }

        void mWorkspaceSystemWatcher_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext should never be deleted - process must be restarted - which will result to an error
            RestartProcess();
        }

        void mWorkspaceSystemWatcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext changed load new setting
            RestartProcess();
        }

        /**
         * @fn  ~Process()
         *
         * @brief   Finaliser.
         *
         * @author  Stefanos Anastasiou
         * @date    10.03.2013
         */
        ~Process()
        {
            Dispose(false);
        }

        /**
         * \fn  private void PortNumberRetrievalCompleted(IAsyncResult ar)
         *
         * \brief   Executed when a backend process starts. Use to check if the port number retrieval is completed.
         *
         * \author  Stefanos Anastasiou
         * \date    31.05.2013
         *
         * \param   ar  The archive.
         */
        private void PortNumberRetrievalCompleted(IAsyncResult ar)
        {
            //called when the getPortNumber method has finished work...    
            mIsPortNumberRetrieved = this.mStartProcessDelegate.EndInvoke(out this.mPort, ar);
            //process start attemp finished - notify subscribers
            if ( ConnectorCreatedEvent != null)
            {
                ConnectorCreatedEvent(this, new ConnectorCreatedEventArgs(this.mPInfo.ProcKey));
            }
            //port could be retrieved - backend is running
            if (this.mPort != -1)
            {
                //load model if specified option is enabled 
                //IOptions aOptions = (IOptions)mMSVSInstance.GetObject("RText.NET");
                //if (aOptions.EnableAutomaticModelLoad)
                //{
                //    this.OnTimerElapsed(null, EventArgs.Empty);
                //}
            }
            writeAutoRunValue(this.mAutoRunKey);
            mAutoRunKey = String.Empty;
        }

        /**
         * @property    public readonly bool IsPortNumberRetrieved
         *
         * @brief   Gets a value indicating whether this object could find it's port number from the backend.
         *
         * @return  true if this object's port number is retrieved, false if not.
         */
        public bool IsPortNumberRetrieved
        {
            get
            {
                return mIsPortNumberRetrieved;
            }
        }

        /**
         * \fn  private bool GetPortNumber(int timeout, out int portNumber)
         *
         * \brief   Gets port number.
         * \note    If the backend process could not be started, a -1 is returned.
         * \author  Stefanos Anastasiou
         * \date    31.05.2013
         *
         * \param   timeout         The timeout.
         * \param [out] portNumber  The port number.
         *
         * \return  true if it succeeds, false if it fails.
         */
        private bool GetPortNumber(int timeout, out int portNumber)
        {
            //will only return true if port number is retrieved during a timeout of 2 seconds!
            if (mTimeoutEvent.WaitOne(Constants.INITIAL_RESPONSE_TIMEOUT))
            {
                if (mConnector == null)
                {
                    //create a new connector if it's the first time for this workspace
                    mConnector = new Connector( this );
                }
                this.mPInfo.Port = portNumber = this.mPort;
                return true;
            }
            else
            {
                CleanupProcess();
                this.mPInfo.Port = portNumber = this.mPort = -1;
                return false;
            }
        }

        /**
         * @property    public Connector Connector
         *
         * @brief   Gets the connector.
         *
         * @return  The connector.
         */
        public Connector Connector { get { return this.mConnector; } }

        /**
         * @fn  public void Kill()
         *
         * @brief   Kills this process.
         *
         * @author  Stefanos Anastasiou
         * @date    04.12.2012
         */
        public void Kill()
        {
            CleanupProcess();
        }

        /**
         * @property    public bool HasExited
         *
         * @brief   Gets a value indicating whether this process has exited.
         *
         * @return  true if this object has exited, false if not.
         */
        public bool HasExited 
        { 
            get 
            {
                try
                {
                    return this.mProcess.HasExited;
                }
                catch
                {
                    return true;
                }
            }
        }

        /**
         * @fn  public void Dispose()
         *
         * @brief   Performs application-defined tasks associated with freeing, releasing, or resetting
         *          unmanaged resources.
         *
         * @author  Stefanos
         * @date    15.11.2012
         *
         * ### summary  Performs application-defined tasks associated with freeing, releasing, or
         *              resetting unmanaged resources.
         */
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Helpers

        /**
         * @fn  private void Dispose(bool disposing)
         *
         * @brief   Releases the unmanaged resources used by the ConnectorManager and optionally releases
         *          the managed resources.
         *
         * @author  Stefanos Anastasiou
         * @date    15.11.2012
         *
         * @param   disposing   true to release both managed and unmanaged resources; false to release
         *                      only unmanaged resources.
         */
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose any disposable fields here
                GC.SuppressFinalize(this);
            }
            ReleaseNativeResources();
        }

        /**
         * @fn  private void ReleaseNativeResources()
         *
         * @brief   Releases the native resources.
         *
         * @author  Stefanos Anastasiou
         * @date    15.11.2012
         */
        private void ReleaseNativeResources()
        {
            CleanupProcess();
        }

        /**
         * @fn  private void CleanupProcess()
         *
         * @brief   Cleanup process.
         * @todo    Send shutdown command instead of killing the process.
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         */
        private void CleanupProcess()
        {
            try
            {
                this.mPort = -1;
                //clean up process here
                mProcess.EnableRaisingEvents = false;
                Utilities.ProcessUtilities.KillProcessTree(mProcess);
                mStdErrTokenSource.Cancel();
                mStdOutTokenSource.Cancel();
                mStdErrReaderTask.Wait( 2000 );
                mStdOutReaderTask.Wait( 2000 );
                
                //todo send shutdown command to rtext service and wait for it to die
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                mFileSystemWatcher.Changed -= FileSystemWatcherChanged;
                mFileSystemWatcher.Deleted -= FileSystemWatcherDeleted;
                mFileSystemWatcher.Created -= FileSystemWatcherChanged;
                mFileSystemWatcher.Renamed -= FileSystemWatcherRenamed;
                mFileSystemWatcher.Error   -= ProcessError;
                mWorkspaceSystemWatcher.Changed -= mWorkspaceSystemWatcher_Changed;
                mWorkspaceSystemWatcher.Created -= mWorkspaceSystemWatcher_Created;
                mWorkspaceSystemWatcher.Deleted -= mWorkspaceSystemWatcher_Deleted;
                mWorkspaceSystemWatcher.Error -= ProcessError;
                mWorkspaceSystemWatcher.Renamed -= mWorkspaceSystemWatcher_Renamed;
                mProcess.Exited            -= OnProcessExited;
            }
        }

        /**
         * @fn  private void ReadStream( StreamReader stream, CancellationToken token)
         *
         * @brief   Reads a synchronous stream asynchornously. .NET bug workaround.
         *
         * @author  Stefanos Anastasiou
         * @date    10.03.2013
         *
         * @param   stream  The stream.
         * @param   token   The cancellation token.
         */
        private void ReadStream(System.IO.StreamReader stream, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            while (!token.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(Constants.OUTPUT_POLL_PERIOD);
                if (!stream.EndOfStream)
                {
                    string aLine = stream.ReadLine();
                    //StatusBarManager.writeToOutputWindow(aLine, mPInfo.Guid.Value, mPInfo.ProcKey);
                    if ( mIsProcessStarting )
                    {
                        if (mBackendInitResponseRegex.IsMatch(aLine))
                        {
                            mPort = Int32.Parse(mBackendInitResponseRegex.Match(aLine).Groups[1].Value);
                            mTimeoutEvent.Set();
                            mIsProcessStarting = false;
                        }
                    }
                }
            }
        }

        /**
         * @fn  private void OnProcessExited(object sender, EventArgs e)
         *
         * @brief   Raises the process exited event.
         *
         * @author  Stefanos Anastasiou
         * @date    10.03.2013
         *
         * @param   sender  Source of the event.
         * @param   e       Event information to send to registered event handlers.
         */
        private void OnProcessExited(object sender, EventArgs e)
        {
            CleanupProcess();
            //notify connectors that their backend in no longer available!
            if (ProcessExitedEvent != null)
            {
                ProcessExitedEvent(this, new ProcessExitedEventArgs( this.mPInfo.ProcKey ) );
            }
        }

        /**
         * \fn  private void RestartLoadModelTimer()
         *
         * \brief   Restarts load model timer.
         *
         * \author  Stefanos Anastasiou
         * \date    11.06.2013
         */
        private void RestartLoadModelTimer()
        {
            mTimer.Stop();
            mTimer.Start();
        }

        /**
         * \fn  private void RestartProcess()
         *
         * \brief   Restart process.
         *
         * \author  Stefanos Anastasiou
         * \date    12.10.2013
         */
        private void RestartProcess()
        {
            try
            {
                OnProcessExited(null, EventArgs.Empty);
                //must re-read .rtext file and restart process
                if (!Utilities.FileUtilities.findRTextFile(out mPInfo, mPInfo.WorkingDirectory, mExtension))
                {
                    throw new Exception(String.Format("Could not restart process for .rtext file in directory : {0} after modification were made to the file.\nPlease check your configuration.", mPInfo.WorkingDirectory));
                }
                StartRTextService();
            }
            catch (Exception ex)
            {
                //StatusBarManager.writeToOutputWindow(String.Format( "ERROR: {0}", ex.Message ),
                //                                     Utilities.HashUtilities.getGUIDfromString(ProcessInfo.ProcKey),
                //                                     ProcessInfo.ProcKey
                //                                    );
                //clean up
                CleanupProcess();
                OnProcessExited(null, EventArgs.Empty);
            }
        }

        private string disabledCmdExeCustomization()
        {
            RegistryKey aCmdKey  = Registry.CurrentUser;
            var aCmdProcessorKey = aCmdKey.OpenSubKey(@"Software\Microsoft\Command Processor", true);
            string aAutorunValue = aCmdProcessorKey.GetValue("autorun") as string;
            writeAutoRunValue(String.Empty);
            return aAutorunValue;
        }

        private void writeAutoRunValue(string value)
        {
            RegistryKey aCmdKey = Registry.CurrentUser;
            var aCmdProcessorKey = aCmdKey.OpenSubKey(@"Software\Microsoft\Command Processor", true);
            aCmdProcessorKey.SetValue("autorun", value != null ? value : String.Empty, RegistryValueKind.String);
        }

        #endregion

        #region Event Handlers

        /**
         * \fn  void ProcessError(object sender, ErrorEventArgs e)
         *
         * \brief   Process erros of FileSystemWatcher objetcs. Re-initialize watchers.
         *
         * \author  Stefanos Anastasiou
         * \date    23.06.2013
         *
         * \param   sender  Source of the event.
         * \param   e       Error event information.
         */
        void ProcessError(object sender, System.IO.ErrorEventArgs e)
        {
            if (ReferenceEquals(sender, mFileSystemWatcher))
            {
                //automate watcher died - restart watcher
                mFileSystemWatcher.Changed -= FileSystemWatcherChanged;
                mFileSystemWatcher.Deleted -= FileSystemWatcherDeleted;
                mFileSystemWatcher.Created -= FileSystemWatcherChanged;
                mFileSystemWatcher.Renamed -= FileSystemWatcherRenamed;
                mFileSystemWatcher.Error -= ProcessError;
                var tempFileWatcher = new FileSystemWatcher(mFileSystemWatcher.Directory, 
                                                            mFileSystemWatcher.HasGUI,
                                                            mFileSystemWatcher.Include,
                                                            mFileSystemWatcher.Exclude,
                                                            mFileSystemWatcher.FilterFlags,
                                                            mFileSystemWatcher.MonitorSubDirectories
                                                           );
                mFileSystemWatcher         = tempFileWatcher;
                mFileSystemWatcher.Changed += FileSystemWatcherChanged;
                mFileSystemWatcher.Deleted += FileSystemWatcherDeleted;
                mFileSystemWatcher.Created += FileSystemWatcherChanged;
                mFileSystemWatcher.Renamed += FileSystemWatcherRenamed;
                mFileSystemWatcher.Error   += ProcessError;
            }
            else
            {
                //.rtext watcher die - restart it
            }
        }

        /**
         * \fn  private void FileSystemWatcherRenamed(object sender, RenamedEventArgs e)
         *
         * \brief   Handle file renaming events.
         *
         * \author  Stefanos Anastasiou
         * \date    23.06.2013
         *
         * \param   sender  Source of the event.
         * \param   e       Renamed event information.
         */
        private void FileSystemWatcherRenamed(object sender, System.IO.RenamedEventArgs e)
        {
            if (this.mConnector != null)
            {
                RestartLoadModelTimer();
            }
        }

        /**
         * \fn  private void FileSystemWatcherDeleted(object sender, FileSystemEventArgs e)
         *
         * \brief   Handle file deletion events.
         *
         * \author  Stefanos Anastasiou
         * \date    23.06.2013
         *
         * \param   sender  Source of the event.
         * \param   e       File system event information.
         */
        private void FileSystemWatcherDeleted(object sender, System.IO.FileSystemEventArgs e)
        {
            if (this.mConnector != null)
            {
                RestartLoadModelTimer();
            }
        }

        /**
         * \fn  private void FileSystemWatcherChanged(object sender, FileSystemEventArgs e)
         *
         * \brief   Handle file saved events.
         *
         * \author  Stefanos Anastasiou
         * \date    23.06.2013
         *
         * \param   sender  Source of the event.
         * \param   e       File system event information.
         */
        private void FileSystemWatcherChanged(object sender, System.IO.FileSystemEventArgs e)
        {
            //is this an automate document and auto save all document is enabled?
            //IOptions aOptions = (IOptions)mMSVSInstance.GetObject("RText.NET");
            if ( e.ChangeType == System.IO.WatcherChangeTypes.Changed)// && aOptions.AutoSaveAllDocuments )
            {
                //find .rtext file of this document 
                string aRTextFilePath = Utilities.FileUtilities.findRTextFile(Utilities.FileUtilities.getDir(e.FullPath), Utilities.FileUtilities.getExt(e.FullPath));
                //foreach (EnvDTE.Document doc in mMSVSInstance.Documents)
                //{
                //    if (!doc.Saved && aRTextFilePath == Utilities.FileUtilities.findRTextFile(Utilities.FileUtilities.getDir(doc.FullName), Utilities.FileUtilities.getExt(doc.FullName)))
                //    {
                //        doc.Save(doc.FullName);
                //    }
                //}
            }
            if (this.mConnector != null)
            {
                RestartLoadModelTimer();
            }
        }

        /**
         * @fn  private void OnTimerElapsed(object sender, EventArgs e)
         *
         * @brief   Event handler. Called when the dispatcher timer expires.
         *
         * @author  Stefanos Anastasiou
         * @date    09.03.2013
         *
         * @param   sender  Source of the event.
         * @param   e       Event information.
         */
        private void OnTimerElapsed(object sender, EventArgs e)
        {
            //check needed so that the interval timer don't stop if a command could not be loaded - this way we can ensure that the complete model will always be loaded!
            if (this.mConnector.ConnectorState == StateEngine.ProcessState.Busy)
            {
                if (!mIsMessageDisplayed)
                {
                    //StatusBarManager.writeToOutputWindow(String.Format("Changes were made to automate files while the model was being loaded. New loading pending..."), Utilities.HashUtilities.getGUIDfromString(this.mPInfo.ProcKey));
                    mIsMessageDisplayed = true;
                }
                return;
            }
            else
            {
                Protocol.RequestBase aLoadCommand = new Protocol.RequestBase { command = Constants.Commands.LOAD_MODEL, invocation_id = -1, type = "request" };
                this.mConnector.execute(aLoadCommand, ref mInvocationId);
                this.mTimer.Stop();
                this.mIsMessageDisplayed = false;
            }
        }
        #endregion
    }
}
