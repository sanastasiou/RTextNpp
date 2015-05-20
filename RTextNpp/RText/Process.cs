using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;
using RTextNppPlugin.RText;
using RTextNppPlugin.RText.Protocol;

namespace RTextNppPlugin.Utilities
{
    /**
     * \class   Process
     *
     * \brief   Process wrapper class over the .NET process class. Fixes several bugs regarding IO redirect.
     *
     */
    public class RTextBackendProcess
    {
        #region Fields

        System.Diagnostics.Process mProcess = null;
        ProcessInfo mPInfo = null;
        FileSystemWactherCLRWrapper.FileSystemWatcher mFileSystemWatcher = null;                                                 //!< Observes all automate files for modifications.
        FileSystemWactherCLRWrapper.FileSystemWatcher mWorkspaceSystemWatcher = null;                                            //!< Observes .rtext file for any modifications.
        CancellationTokenSource mStdOutTokenSource = null;
        CancellationToken mStdOutToken;
        CancellationTokenSource mStdErrTokenSource = null;
        CancellationToken mStdErrToken;
        Task mStdOutReaderTask = null;
        Task mStdErrReaderTask = null;
        bool mIsProcessStarting = true;
        int mPort = -1;
        Connector mConnector = null;
        readonly Regex mBackendInitResponseRegex = new Regex(@"^RText service, listening on port (\d+)$", RegexOptions.Compiled);
        ManualResetEvent mTimeoutEvent = new ManualResetEvent(false);
        DispatcherTimer mTimer;
        bool mIsMessageDisplayed = false;
        IAsyncResult mGetPortNumberAsyncResult;
        bool mIsPortNumberRetrieved = false;                                                                                     //!< Whether a process has started and the port number is retrieved.
        string mExtension = String.Empty;                                                                                        //!< The associated extension string.
        string mAutoRunKey = String.Empty;                                                                                       //!< The autorun registry value.

        /**
         *
         * \brief   Asynchronous method caller. Used to run the process start up asynchronously so that the UI thread doesn't get blocked.
         *
         * \param   timeout         The timeout.
         * \param [out] portNumber  The port number.
         *
         * \return  Indicates whether asyn caller was successfull.
         */
        public delegate bool AsyncMethodCaller(int timeout, out int portNumber);
        AsyncMethodCaller mStartProcessDelegate;                                                                                                               //!< The start process delegate
        #endregion

        #region Interface

        #region Nested Types

        /**
         * \class   ProcessInfo
         * 
         * \brief   Information needed to start a backend process.
         * 
         */
        public class ProcessInfo
        {
            /**
             *
             * \brief   Constructor.
             *
             *
             * \param   workingDir  The working dir.
             * \param   rtextPath   Full pathname of the rtext file.
             * \param   cmdLine     The command line.
             * \param   key         The process key.
             * \param   port        The port number associated with this process.
             */
            public ProcessInfo(string workingDir, string rtextPath, string cmdLine, string key, int port = -1)
            {
                WorkingDirectory = workingDir;
                RTextFilePath = rtextPath;
                CommandLine = cmdLine;
                ProcKey = key;
                Port = port;
                Name = null;
            }

            /**
             * \property    public string workingDirectory
             *
             * \brief   Gets or sets the pathname of the working directory.
             *
             * \return  The pathname of the working directory.
             */
            public string WorkingDirectory { get; private set; }

            /**
             * \property    public string rTextFilePath
             *
             * \brief   Gets or sets the full pathname of the text file.
             *
             * \return  The full pathname of the text file.
             */
            public string RTextFilePath { get; private set; }

            /**
             * \property    public string commandLine
             *
             * \brief   Gets or sets the command line.
             *
             * \return  The command line.
             */
            public string CommandLine { get; private set; }

            /**
             * \property    public string procKey
             *
             * \brief   Gets or sets a unique proc key based on the location of the rtxet file and the associated extensions
             *
             * \return  The proc key.
             */
            public string ProcKey { get; private set; }

            /**
             * \property    public int Port
             *
             * \brief   Gets or sets the port.
             *
             * \return  The port.
             */
            public int Port { get; set; }

            /**
             * \property    public string Name
             *
             * \brief   Gets or sets the name of the process.
             *
             * \return  The name.
             */
            public string Name { get; set; }

            /**
             * \property    public string Extension
             *
             * \brief   Gets or sets the extension for which a backend process was started.
             *
             * \return  The extension.
             */
            public string Extension { get; set; }
        };
        #endregion

        #region Events
        /**
         *
         * \brief   Delegate for the CommandCompleted event.
         *
         *
         * \param   source  Source object of the event.
         * \param   e       Command completed event information.
         */
        public delegate void ProcessExited(object source, ProcessExitedEventArgs e);

        public event ProcessExited ProcessExitedEvent;                                                                                      //!< Event queue for all listeners interested in ProcessExited events.

        /**
         * \class   CommandCompletedEventArgs
         *
         * \brief   Command completed is an event which is fired to notify subscribers that an async command has finished executing.
         *
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
         * Constructor.
         *
         * \param   rTextFilePath   Full pathname of the text file.
         * \param   ext             The extension of the rtext file. 
         * \remark  For each extension, even of the same .rtext file, a different process should be started.                         
         */
        public RTextBackendProcess(string rTextFilePath, string ext)
            : this(rTextFilePath, Path.GetDirectoryName(rTextFilePath), GetCommandLine(rTextFilePath, ext), rTextFilePath + ext, ext)
        {
            mConnector = new Connector(this);            
            StartRTextService();
        }

        /**
         * Gets the proc key.
         *
         * \return  The proc key. The process key is the complete filepath of the .rtext file that started this process with the addition of the file extensions that are associated with this process e.g. .atm. This way
         *          It is guaranteed that the key of a process is a unique identifier.
         */
        public string ProcKey
        {
            get
            {
                return mPInfo.ProcKey;
            }
        }

        /**
         * Gets the port.
         *
         * \return  The port. This port is being reported by the backend process upon startup, and it is used by the fronted to establich communication with the backend via sockets.
         */
        public int Port
        {
            get
            {
                return mPInfo.Port;
            }
        }

        public string Workspace
        {
            get
            {
                return (mPInfo != null ? mPInfo.ProcKey : null);
            }
        }

        /**
         *
         * \brief   Tries to start the backend service, as it is specified in the .rtext file.
         *
         *
         * \return  true if it succeeds, false if it fails.
         */
        public void StartRTextService()
        {
            if (mGetPortNumberAsyncResult == null || mGetPortNumberAsyncResult.IsCompleted)
            {
                //process was never started or has already been started and stopped
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
                mFileSystemWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher(System.IO.Path.GetDirectoryName(mPInfo.RTextFilePath),
                                                                                        false,
                                                                                        aExtensionsFilter,
                                                                                        String.Empty,
                                                                                        (uint)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime),
                                                                                        true);
                mFileSystemWatcher.Changed += FileSystemWatcherChanged;
                mFileSystemWatcher.Deleted += FileSystemWatcherDeleted;
                mFileSystemWatcher.Created += FileSystemWatcherChanged;
                mFileSystemWatcher.Renamed += FileSystemWatcherRenamed;
                mFileSystemWatcher.Error += ProcessError;
                //finally add .rtext wacther
                mWorkspaceSystemWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher(System.IO.Path.GetDirectoryName(mPInfo.RTextFilePath),
                                                                                             false,
                                                                                             "*" + Constants.WORKSPACE_TYPE,
                                                                                             String.Empty,
                                                                                             (uint)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime),
                                                                                             false
                                                                                           );
                mWorkspaceSystemWatcher.Changed += OnWorkspaceSystemWatcherChanged;
                mWorkspaceSystemWatcher.Deleted += OnWorkspaceSystemWatcherDeleted;
                mWorkspaceSystemWatcher.Created += OnWorkspaceSystemWatcherCreated;
                mWorkspaceSystemWatcher.Renamed += OnWorkspaceSystemWatcherRenamed;
                mWorkspaceSystemWatcher.Error += ProcessError;

                mProcess.Exited += new EventHandler(OnProcessExited);
                //disable doskey or whatever actions are associated with cmd.exe
                mAutoRunKey = DisableCmdExeCustomization();
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

                mStartProcessDelegate = new AsyncMethodCaller(GetPortNumber);
                mPort = -1; //reinit port every time this function is called
                mGetPortNumberAsyncResult = mStartProcessDelegate.BeginInvoke( Constants.INITIAL_RESPONSE_TIMEOUT,
                                                                               out mPort,
                                                                               new AsyncCallback(PortNumberRetrievalCompleted),
                                                                               mStartProcessDelegate);
            }
        }

        /**
         *
         * \brief   Executed when a backend process starts. Use to check if the port number retrieval is completed.
         *
         *
         * \param   ar  The archive.
         */
        private void PortNumberRetrievalCompleted(IAsyncResult ar)
        {
            //called when the getPortNumber method has finished work...    
            mIsPortNumberRetrieved = mStartProcessDelegate.EndInvoke(out mPort, ar);

            //port could be retrieved - backend is running
            if (mPort != -1)
            {
                //load model if specified option is enabled 
                if (Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoLoadWorkspace))
                {
                    OnTimerElapsed(null, EventArgs.Empty);
                }
            }
            WriteAutoRunValue(mAutoRunKey);
            mAutoRunKey = String.Empty;
        }

        /**
         * \property    public readonly bool IsPortNumberRetrieved
         *
         * \brief   Gets a value indicating whether this object could find it's port number from the backend.
         *
         * \return  true if this object's port number is retrieved, false if not.
         */
        public bool IsPortNumberRetrieved
        {
            get
            {
                return mIsPortNumberRetrieved;
            }
        }

        /**
         *
         * \brief   Gets port number.
         * \note    If the backend process could not be started, a -1 is written in the portNumber.
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
                mPInfo.Port = portNumber = mPort;
                return true;
            }
            else
            {
                CleanupProcess();
                mPInfo.Port = portNumber = mPort = -1;
                return false;
            }
        }

        /**
         * \property    public Connector Connector
         *
         * \brief   Gets the connector.
         *
         * \return  The connector.
         */
        public Connector Connector { get { return mConnector; } }

        /**
         *
         * \brief   Kills this process.
         *
         */
        public void Kill()
        {
            CleanupProcess();
        }

        /**
         * \property    public bool HasExited
         *
         * \brief   Gets a value indicating whether this process has exited.
         *
         * \return  true if this object has exited, false if not.
         */
        public bool HasExited
        {
            get
            {
                try
                {
                    return mProcess.HasExited;
                }
                catch
                {
                    return true;
                }
            }
        }

        #endregion

        #region Implementation Details

        /**
         *
         * \brief   Constructor.
         *
         *
         * \param   rTextFilePath       Full pathname of the .rtext file.
         * \param   workingDirectory    Pathname of the working directory.
         * \param   commandLine         The command line.
         * \param   processKey          The process key.
         */
        public RTextBackendProcess(string rTextFilePath, string workingDirectory, string commandLine, string processKey, string extenstion)
        {
            mPInfo = new ProcessInfo(workingDirectory, rTextFilePath, commandLine, processKey);
            mTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                //wait one second before loading model in case of thousands of events / sec
                Interval = TimeSpan.FromMilliseconds(50)
            };
            mTimer.Tick += OnTimerElapsed;
            mExtension = extenstion;
        }
       
        /**
         *
         * \brief   Cleanup process.
         * \todo    Send shutdown command instead of killing the process.
         */
        public void CleanupProcess()
        {
            try
            {
                mPort = -1;
                //clean up process here
                mProcess.EnableRaisingEvents = false;
                Utilities.ProcessUtilities.KillProcessTree(mProcess);
                mStdErrTokenSource.Cancel();
                mStdOutTokenSource.Cancel();
                mStdErrReaderTask.Wait(2000);
                mStdOutReaderTask.Wait(2000);

                //todo send shutdown command to rtext service and wait for it to die
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            finally
            {
                if (mFileSystemWatcher != null)
                {
                    mFileSystemWatcher.Changed -= FileSystemWatcherChanged;
                    mFileSystemWatcher.Deleted -= FileSystemWatcherDeleted;
                    mFileSystemWatcher.Created -= FileSystemWatcherChanged;
                    mFileSystemWatcher.Renamed -= FileSystemWatcherRenamed;
                    mFileSystemWatcher.Error -= ProcessError;
                }
                if (mWorkspaceSystemWatcher != null)
                {
                    mWorkspaceSystemWatcher.Changed -= OnWorkspaceSystemWatcherChanged;
                    mWorkspaceSystemWatcher.Created -= OnWorkspaceSystemWatcherCreated;
                    mWorkspaceSystemWatcher.Deleted -= OnWorkspaceSystemWatcherDeleted;
                    mWorkspaceSystemWatcher.Error -= ProcessError;
                    mWorkspaceSystemWatcher.Renamed -= OnWorkspaceSystemWatcherRenamed;
                }
                if (mProcess != null)
                {
                    mProcess.Exited -= OnProcessExited;
                }
            }
        }

        /**
         *
         * \brief   Reads a synchronous stream asynchornously. .NET bug workaround.
         *
         *
         * \param   stream  The stream.
         * \param   token   The cancellation token.
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
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, mPInfo.ProcKey, aLine);
                    if (mIsProcessStarting)
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
         *
         * \brief   Raises the process exited event.
         *
         *
         * \param   sender  Source of the event.
         * \param   e       Event information to send to registered event handlers.
         */
        private void OnProcessExited(object sender, EventArgs e)
        {
            CleanupProcess();
            //notify connectors that their backend in no longer available!
            if (ProcessExitedEvent != null)
            {
                ProcessExitedEvent(this, new ProcessExitedEventArgs(mPInfo.ProcKey));
            }
        }

        /**

         *
         * \brief   Restarts load model timer.
         *
         */
        private void RestartLoadModelTimer()
        {
            mTimer.Stop();
            mTimer.Start();
        }

        /**
         *
         * \brief   Restart process. Occurs when .rtext file is modified.
         *                   
         */
        private void RestartProcess()
        {
            try
            {
                OnProcessExited(null, EventArgs.Empty);
                if (File.Exists(mPInfo.RTextFilePath))
                {
                    string cmdLine = GetCommandLine(mPInfo.RTextFilePath, mExtension);
                    if (cmdLine != null)
                    {
                        mPInfo = new ProcessInfo(mPInfo.WorkingDirectory, mPInfo.RTextFilePath, mPInfo.CommandLine, mPInfo.ProcKey);
                    }
                    else
                    {
                        Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, mPInfo.ProcKey, "Could not read command line for extension {1} from file : {0} after modifications were made to the file.", mPInfo.RTextFilePath, mExtension);
                        return;
                    }
                }
                else
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, mPInfo.ProcKey, "Could not locate file : {0} after modifications were made to the file.", mPInfo.RTextFilePath);
                    return;
                }
                StartRTextService();
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, mPInfo.ProcKey, "Process.RestartProcess : Exception : {0}", ex.Message);
                //clean up
                CleanupProcess();
                OnProcessExited(null, EventArgs.Empty);
            }
        }

        /**
         * Disables the command executable customization because some broken programs throw exceptions when one tries to start a program from the command line.
         *
         * \return  A string containing the cmd auto run customization command.
         */
        private string DisableCmdExeCustomization()
        {
            RegistryKey aCmdKey = Registry.CurrentUser;
            var aCmdProcessorKey = aCmdKey.OpenSubKey(@"Software\Microsoft\Command Processor", true);
            string aAutorunValue = aCmdProcessorKey.GetValue("autorun") as string;
            WriteAutoRunValue(String.Empty);
            return aAutorunValue;
        }

        /**
         * Restore command line customization.
         *
         * \param   value   The string containing a previously stored auto run customization command.
         */
        private void WriteAutoRunValue(string value)
        {
            RegistryKey aCmdKey = Registry.CurrentUser;
            var aCmdProcessorKey = aCmdKey.OpenSubKey(@"Software\Microsoft\Command Processor", true);
            aCmdProcessorKey.SetValue("autorun", value != null ? value : String.Empty, RegistryValueKind.String);
        }

        /**
         * Gets command line.
         *
         * \param   rtextFilePath   Full pathname of the .rtext file.
         * \param   extension       The extension for which the command line should be retrieved.
         *
         * \return  The command line.
         */
        private static string GetCommandLine(string rtextFilePath, string extension)
        {
            try
            {
                string[] aLines = File.ReadAllLines(rtextFilePath);
                if (aLines.Count() != 0)
                {

                    for (int i = 0; i < aLines.Count(); ++i)
                    {
                        //skip empty lines
                        if (String.IsNullOrEmpty(aLines[i])) continue;
                        //find endings
                        Match matchResults = FileUtilities.FileExtensionRegex.Match(aLines[i]);
                        bool aHasFoundMatch = false;
                        while (matchResults.Success)
                        {
                            if (matchResults.Value.Equals(extension))
                            {
                                aHasFoundMatch = true;
                                break;
                            }
                            matchResults = matchResults.NextMatch();
                        }
                        //ok found matching extension in .rtext file, check for commandline - next line should be the command line
                        if (aHasFoundMatch && (i + 1 < aLines.Count()) && !String.IsNullOrEmpty(aLines[i + 1]))
                        {
                            return aLines[i + 1];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "Process.GetCommandLine({0}, {1}) - Exception : {2}", rtextFilePath, extension, ex.Message);
            }
            return null;
        }

        #endregion

        #region Event Handlers

        void OnWorkspaceSystemWatcherRenamed(object sender, System.IO.RenamedEventArgs e)
        {
            //.rtext should never be renamed - process must be restarted - which will result to an error
            RestartProcess();
        }

        void OnWorkspaceSystemWatcherCreated(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext should never be recreated - process must be restarted
            RestartProcess();
        }

        void OnWorkspaceSystemWatcherDeleted(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext should never be deleted - process must be restarted - which will result to an error
            RestartProcess();
        }

        void OnWorkspaceSystemWatcherChanged(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext changed load new setting
            RestartProcess();
        }

        /**
         *
         * \brief   Process erros of FileSystemWatcher objetcs. Re-initialize watchers.
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
                var tempFileWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher(mFileSystemWatcher.Directory,
                                                                                        mFileSystemWatcher.HasGUI,
                                                                                        mFileSystemWatcher.Include,
                                                                                        mFileSystemWatcher.Exclude,
                                                                                        mFileSystemWatcher.FilterFlags,
                                                                                        mFileSystemWatcher.MonitorSubDirectories
                                                                                       );
                mFileSystemWatcher = tempFileWatcher;
                mFileSystemWatcher.Changed += FileSystemWatcherChanged;
                mFileSystemWatcher.Deleted += FileSystemWatcherDeleted;
                mFileSystemWatcher.Created += FileSystemWatcherChanged;
                mFileSystemWatcher.Renamed += FileSystemWatcherRenamed;
                mFileSystemWatcher.Error += ProcessError;
            }
            else
            {
                //.rtext watcher die - restart it
            }
        }

        /**
         *
         * \brief   Handle file renaming events.         
         *
         * \param   sender  Source of the event.
         * \param   e       Renamed event information.
         */
        private void FileSystemWatcherRenamed(object sender, System.IO.RenamedEventArgs e)
        {
            OnWorkspaceModified(e.OldFullPath);
        }

        /**
         *
         * \brief   Handle file deletion events.
         *
         *
         * \param   sender  Source of the event.
         * \param   e       File system event information.
         */
        private void FileSystemWatcherDeleted(object sender, System.IO.FileSystemEventArgs e)
        {
            OnWorkspaceModified(e.FullPath);
        }

        /**

         *
         * \brief   Handle file saved events.
         *
         *
         * \param   sender  Source of the event.
         * \param   e       File system event information.
         * \todo    Automatically save workspace before reloading the backend.                  
         */
        private void FileSystemWatcherChanged(object sender, System.IO.FileSystemEventArgs e)
        {
            OnWorkspaceModified(e.FullPath);
        }

        /**
         * Executes the workspace modified action.
         *
         * \param   pathOfModifiedFile  The path of modified file.
         */
        private void OnWorkspaceModified(string pathOfModifiedFile)
        {
            if (Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoSaveFiles))
            {
                //find .rtext file of this document 
                string aRTextFilePath = FileUtilities.FindWorkspaceRoot(pathOfModifiedFile);
                Plugin.GetFileObserver().SaveWorkspaceFiles(aRTextFilePath);
            }
            if (mConnector != null)
            {
                RestartLoadModelTimer();
            }
        }

        /**
         *
         * \brief   Event handler. Called when the dispatcher timer expires.
         *         
         * \param   sender  Source of the event.
         * \param   e       Event information.
         */
        private void OnTimerElapsed(object sender, EventArgs e)
        {
            //check needed so that the interval timer don't stop if a command could not be loaded - this way we can ensure that the complete model will always be loaded!
            if (mConnector.ConnectorState == RText.StateEngine.ProcessState.Busy)
            {
                if (!mIsMessageDisplayed)
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, mPInfo.ProcKey, "Changes were made to automate files while the model was being loaded. New loading pending...", mPInfo.ProcKey);
                    mIsMessageDisplayed = true;
                }
                return;
            }
            else
            {
                mConnector.LoadModel();
                mTimer.Stop();
                mIsMessageDisplayed = false;
            }
        }
        #endregion
    }
}
