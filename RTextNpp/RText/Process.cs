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

namespace RTextNppPlugin.Utilities
{
    using RTextNppPlugin.Utilities.Settings;
    /**
     * \class   RTextBackendProcess
     *
     * \brief   Process wrapper class over the .NET process class. Fixes several bugs regarding IO redirect.
     *
     */
    public class RTextBackendProcess
    {
        #region [Data _embers]

        System.Diagnostics.Process _Process = null;
        ProcessInfo _PInfo = null;
        FileSystemWactherCLRWrapper.FileSystemWatcher _FileSystemWatcher = null;                                                 //!< Observes all rtext files for _odifications.
        FileSystemWactherCLRWrapper.FileSystemWatcher _WorkspaceSystemWatcher = null;                                            //!< Observes .rtext file for any _odifications.
        ISettings _settings = null;                                                                                              //!< Allows access to persistent settings.
        CancellationTokenSource _StdOutTokenSource = null;
        CancellationToken _StdOutToken;
        CancellationTokenSource _StdErrTokenSource = null;
        CancellationToken _StdErrToken;
        Task _StdOutReaderTask = null;
        Task _StdErrReaderTask = null;
        bool _IsProcessStarting = true;
        int _Port = -1;
        Connector _Connector = null;
        readonly Regex _BackendInitResponseRegex = new Regex(@"^RText service, listening on port (\d+)$", RegexOptions.Compiled);
        ManualResetEvent _TimeoutEvent = new ManualResetEvent(false);
        DispatcherTimer _Timer;
        bool _IsMessageDisplayed = false;
        IAsyncResult _GetPortNumberAsyncResult;
        bool _IsPortNumberRetrieved = false;                                                                                     //!< Whether a process has started and the port number is retrieved.
        string _Extension = String.Empty;                                                                                        //!< The associated extension string.
        string _AutoRunKey = String.Empty;                                                                                       //!< The autorun registry value.

        /**
         *
         * \brief   Asynchronous _ethod caller. Used to run the process start up asynchronously so that the UI thread doesn't get blocked.
         *
         * \param   timeout         The timeout.
         * \param [out] portNumber  The port number.
         *
         * \return  Indicates whether asyn caller was successfull.
         */
        public delegate bool AsyncMethodCaller(int timeout, out int portNumber);
        AsyncMethodCaller _StartProcessDelegate;                                                                                                               //!< The start process delegate
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
         * \brief   Constructor.
         *
         * \param   rTextFilePath   Full pathname of the text file.
         * \param   ext             The extension of the rtext file. \remark  For each extension, even of
         *                          the same .rtext file, a different process should be started.
         * \param   settings        Allows access to persistent settings.
         */
        internal RTextBackendProcess(string rTextFilePath, string ext, ISettings settings)
            : this(rTextFilePath, Path.GetDirectoryName(rTextFilePath), GetCommandLine(rTextFilePath, ext), rTextFilePath + ext, ext)
        {
            _settings = settings;
            _Connector = new Connector(this);            
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
                return _PInfo.ProcKey;
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
                return _PInfo.Port;
            }
        }

        public string Workspace
        {
            get
            {
                return (_PInfo != null ? _PInfo.ProcKey : null);
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
            if (_GetPortNumberAsyncResult == null || _GetPortNumberAsyncResult.IsCompleted)
            {
                //process was never started or has already been started and stopped
                Match aMatch = Regex.Match(_PInfo.CommandLine, @"(^\s*\S+)(.*)", RegexOptions.Compiled);
                System.Diagnostics.ProcessStartInfo aProcessStartInfo = new ProcessStartInfo(aMatch.Groups[1].Value, aMatch.Groups[2].Value);
                _PInfo.Name = aMatch.Groups[1].Value;
                aProcessStartInfo.CreateNoWindow = true;
                aProcessStartInfo.RedirectStandardError = true;
                aProcessStartInfo.RedirectStandardOutput = true;
                aProcessStartInfo.UseShellExecute = false;
                aProcessStartInfo.WorkingDirectory = _PInfo.WorkingDirectory;
                _Process = new System.Diagnostics.Process();
                _Process.StartInfo = aProcessStartInfo;
                //add filewacthers for .rtext file and all associated extensions
                string aExtensions = _PInfo.ProcKey.Substring(_PInfo.RTextFilePath.Length, _PInfo.ProcKey.Length - _PInfo.RTextFilePath.Length);
                Regex regexObj = new Regex(@"\.\w+");
                Match matchResults = regexObj.Match(aExtensions);
                string aExtensionsFilter = String.Empty;
                while (matchResults.Success)
                {
                    aExtensionsFilter += "*" + matchResults.Value + ";";
                    matchResults = matchResults.NextMatch();
                }
                aExtensionsFilter = aExtensionsFilter.Substring(0, aExtensionsFilter.Length - 1);
                _FileSystemWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher(System.IO.Path.GetDirectoryName(_PInfo.RTextFilePath),
                                                                                        false,
                                                                                        aExtensionsFilter,
                                                                                        String.Empty,
                                                                                        (uint)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime),
                                                                                        true);
                _FileSystemWatcher.Changed += FileSystemWatcherChanged;
                _FileSystemWatcher.Deleted += FileSystemWatcherDeleted;
                _FileSystemWatcher.Created += FileSystemWatcherChanged;
                _FileSystemWatcher.Renamed += FileSystemWatcherRenamed;
                _FileSystemWatcher.Error += ProcessError;
                //finally add .rtext wacther
                _WorkspaceSystemWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher(System.IO.Path.GetDirectoryName(_PInfo.RTextFilePath),
                                                                                             false,
                                                                                             "*" + Constants.WORKSPACE_TYPE,
                                                                                             String.Empty,
                                                                                             (uint)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime),
                                                                                             false
                                                                                           );
                _WorkspaceSystemWatcher.Changed += OnWorkspaceSystemWatcherChanged;
                _WorkspaceSystemWatcher.Deleted += OnWorkspaceSystemWatcherDeleted;
                _WorkspaceSystemWatcher.Created += OnWorkspaceSystemWatcherCreated;
                _WorkspaceSystemWatcher.Renamed += OnWorkspaceSystemWatcherRenamed;
                _WorkspaceSystemWatcher.Error += ProcessError;

                _Process.Exited += new EventHandler(OnProcessExited);
                //disable doskey or whatever actions are associated with cmd.exe
                _AutoRunKey = DisableCmdExeCustomization();
                _Process.Start();

                _Process.EnableRaisingEvents = true;
                //start reaading asynchronously with tasks
                _StdOutTokenSource = new CancellationTokenSource();
                _StdOutToken = _StdOutTokenSource.Token;
                _StdErrTokenSource = new CancellationTokenSource();
                _StdErrToken = _StdErrTokenSource.Token;
                _StdOutReaderTask = Task.Factory.StartNew(() => ReadStream(_Process.StandardOutput, _StdOutToken), _StdOutToken);
                _StdErrReaderTask = Task.Factory.StartNew(() => ReadStream(_Process.StandardError, _StdErrToken), _StdErrToken);
                _IsProcessStarting = true;
                _TimeoutEvent.Reset();

                _StartProcessDelegate = new AsyncMethodCaller(GetPortNumber);
                _Port = -1; //reinit port every time this function is called
                _GetPortNumberAsyncResult = _StartProcessDelegate.BeginInvoke( Constants.INITIAL_RESPONSE_TIMEOUT,
                                                                               out _Port,
                                                                               new AsyncCallback(PortNumberRetrievalCompleted),
                                                                               _StartProcessDelegate);
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
            //called when the getPortNumber _ethod has finished work...    
            _IsPortNumberRetrieved = _StartProcessDelegate.EndInvoke(out _Port, ar);

            //port could be retrieved - backend is running
            if (_Port != -1)
            {
                //load _odel if specified option is enabled 
                if (_settings.Get<bool>(Settings.Settings.RTextNppSettings.AutoLoadWorkspace))
                {
                    OnTimerElapsed(null, EventArgs.Empty);
                }
            }
            WriteAutoRunValue(_AutoRunKey);
            _AutoRunKey = String.Empty;
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
                return _IsPortNumberRetrieved;
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
            if (_TimeoutEvent.WaitOne(Constants.INITIAL_RESPONSE_TIMEOUT))
            {
                _PInfo.Port = portNumber = _Port;
                return true;
            }
            else
            {
                CleanupProcess();
                _PInfo.Port = portNumber = _Port = -1;
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
        public Connector Connector { get { return _Connector; } }

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
                    return _Process.HasExited;
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
            _PInfo = new ProcessInfo(workingDirectory, rTextFilePath, commandLine, processKey);
            _Timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                //wait one second before loading _odel in case of thousands of events / sec
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _Timer.Tick += OnTimerElapsed;
            _Extension = extenstion;
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
                _Port = -1;
                //clean up process here
                _Process.EnableRaisingEvents = false;
                Utilities.ProcessUtilities.KillProcessTree(_Process);
                _StdErrTokenSource.Cancel();
                _StdOutTokenSource.Cancel();
                _StdErrReaderTask.Wait(2000);
                _StdOutReaderTask.Wait(2000);

                //todo send shutdown command to rtext service and wait for it to die
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            finally
            {
                if (_FileSystemWatcher != null)
                {
                    _FileSystemWatcher.Changed -= FileSystemWatcherChanged;
                    _FileSystemWatcher.Deleted -= FileSystemWatcherDeleted;
                    _FileSystemWatcher.Created -= FileSystemWatcherChanged;
                    _FileSystemWatcher.Renamed -= FileSystemWatcherRenamed;
                    _FileSystemWatcher.Error -= ProcessError;
                }
                if (_WorkspaceSystemWatcher != null)
                {
                    _WorkspaceSystemWatcher.Changed -= OnWorkspaceSystemWatcherChanged;
                    _WorkspaceSystemWatcher.Created -= OnWorkspaceSystemWatcherCreated;
                    _WorkspaceSystemWatcher.Deleted -= OnWorkspaceSystemWatcherDeleted;
                    _WorkspaceSystemWatcher.Error -= ProcessError;
                    _WorkspaceSystemWatcher.Renamed -= OnWorkspaceSystemWatcherRenamed;
                }
                if (_Process != null)
                {
                    _Process.Exited -= OnProcessExited;
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
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, _PInfo.ProcKey, aLine);
                    if (_IsProcessStarting)
                    {
                        if (_BackendInitResponseRegex.IsMatch(aLine))
                        {
                            _Port = Int32.Parse(_BackendInitResponseRegex.Match(aLine).Groups[1].Value);
                            _TimeoutEvent.Set();
                            _IsProcessStarting = false;
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
                ProcessExitedEvent(this, new ProcessExitedEventArgs(_PInfo.ProcKey));
            }
        }

        /**

         *
         * \brief   Restarts load _odel timer.
         *
         */
        private void RestartLoadModelTimer()
        {
            _Timer.Stop();
            _Timer.Start();
        }

        /**
         *
         * \brief   Restart process. Occurs when .rtext file is _odified.
         *                   
         */
        private void RestartProcess()
        {
            try
            {
                OnProcessExited(null, EventArgs.Empty);
                if (File.Exists(_PInfo.RTextFilePath))
                {
                    string cmdLine = GetCommandLine(_PInfo.RTextFilePath, _Extension);
                    if (cmdLine != null)
                    {
                        _PInfo = new ProcessInfo(_PInfo.WorkingDirectory, _PInfo.RTextFilePath, _PInfo.CommandLine, _PInfo.ProcKey);
                    }
                    else
                    {
                        Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, _PInfo.ProcKey, "Could not read command line for extension {1} from file : {0} after _odifications were _ade to the file.", _PInfo.RTextFilePath, _Extension);
                        return;
                    }
                }
                else
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, _PInfo.ProcKey, "Could not locate file : {0} after _odifications were _ade to the file.", _PInfo.RTextFilePath);
                    return;
                }
                StartRTextService();
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, _PInfo.ProcKey, "Process.RestartProcess : Exception : {0}", ex.Message);
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
                        //ok found _atching extension in .rtext file, check for commandline - next line should be the command line
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
            //.rtext should never be renamed - process _ust be restarted - which will result to an error
            RestartProcess();
        }

        void OnWorkspaceSystemWatcherCreated(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext should never be recreated - process _ust be restarted
            RestartProcess();
        }

        void OnWorkspaceSystemWatcherDeleted(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext should never be deleted - process _ust be restarted - which will result to an error
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
            if (ReferenceEquals(sender, _FileSystemWatcher))
            {
                //automate watcher died - restart watcher
                _FileSystemWatcher.Changed -= FileSystemWatcherChanged;
                _FileSystemWatcher.Deleted -= FileSystemWatcherDeleted;
                _FileSystemWatcher.Created -= FileSystemWatcherChanged;
                _FileSystemWatcher.Renamed -= FileSystemWatcherRenamed;
                _FileSystemWatcher.Error -= ProcessError;
                var tempFileWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher(_FileSystemWatcher.Directory,
                                                                                        _FileSystemWatcher.HasGUI,
                                                                                        _FileSystemWatcher.Include,
                                                                                        _FileSystemWatcher.Exclude,
                                                                                        _FileSystemWatcher.FilterFlags,
                                                                                        _FileSystemWatcher.MonitorSubDirectories
                                                                                       );
                _FileSystemWatcher = tempFileWatcher;
                _FileSystemWatcher.Changed += FileSystemWatcherChanged;
                _FileSystemWatcher.Deleted += FileSystemWatcherDeleted;
                _FileSystemWatcher.Created += FileSystemWatcherChanged;
                _FileSystemWatcher.Renamed += FileSystemWatcherRenamed;
                _FileSystemWatcher.Error += ProcessError;
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
         * Executes the workspace _odified action.
         *
         * \param   pathOfModifiedFile  The path of _odified file.
         */
        private void OnWorkspaceModified(string pathOfModifiedFile)
        {
            if (_settings.Get<bool>(Settings.Settings.RTextNppSettings.AutoSaveFiles))
            {
                //find .rtext file of this document 
                string aRTextFilePath = FileUtilities.FindWorkspaceRoot(pathOfModifiedFile);
                Plugin.GetFileObserver().SaveWorkspaceFiles(aRTextFilePath);
            }
            if (_Connector != null)
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
            //check needed so that the interval timer don't stop if a command could not be loaded - this way we can ensure that the complete _odel will always be loaded!
            if (_Connector.CurrentState.State == RText.StateEngine.ConnectorStates.Loading)
            {
                if (!_IsMessageDisplayed)
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, _PInfo.ProcKey, "Changes were _ade to automate files while the _odel was being loaded. New loading pending...", _PInfo.ProcKey);
                    _IsMessageDisplayed = true;
                }
                return;
            }
            else
            {
                _Connector.BeginExecute(Connector.LOAD_COMMAND, RText.StateEngine.Command.LoadModel);
                _Timer.Stop();
                _IsMessageDisplayed = false;
            }
        }
        #endregion
    }
}
