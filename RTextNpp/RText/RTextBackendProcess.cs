using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Collections.Generic;
using AJ.Common;

namespace RTextNppPlugin.RText
{
    using RTextNppPlugin.Utilities;
    using RTextNppPlugin.Utilities.Settings;
    using RTextNppPlugin.Utilities.Threading;
    using RTextNppPlugin.RText.Protocol;

    /**
     * \class   RTextBackendProcess
     *
     * \brief   Process wrapper class over the .NET process class. Fixes several bugs regarding IO redirect.
     *
     */
    internal class RTextBackendProcess
    {
        #region [Data Members]
        private System.Diagnostics.Process _process = null;
        private ProcessInfo _pInfo = null;
        private Windows.Clr.FileWatcher _fileSystemWatcher = null;                                                                       //!< Observes all RText files for modifications.
        private Windows.Clr.FileWatcher _workspaceSystemWatcher = null;                                                                  //!< Observes .rtext file for any modifications.
        private ISettings _settings = null;                                                                                              //!< Allows access to persistent settings.
        private CancellationTokenSource _cancellationSource = null;
        private Task _stdOutReaderTask = null;
        private Task _stdErrReaderTask = null;
        private Connector _connector = null;
        private readonly Regex _backendInitResponseRegex = new Regex(@"^RText service, listening on port (\d+)$", RegexOptions.Compiled);
        private DispatcherTimer _timer;
        private bool _isMessageDisplayed = false;
        private string _extension = String.Empty;                                                                                        //!< The associated extension string.
        private string _autoRunKey = String.Empty;                                                                                       //!< The autorun registry value.
        private readonly VoidDelayedEventHandler _workspaceFileWatcherDebouncer = null;                                                  //!< Debounces workspace file (.rtext) changes events.
        private bool _isShutingDown = false;
        #endregion
        
        #region [Interface]
        
        #region [Nested Types]
        /**
         * \class   ProcessInfo
         *
         * \brief   Information needed to start a back-end process.
         *
         */
        public class ProcessInfo
        {
            /**
             *
             * \brief   Constructor.
             *
             *
             * \param   workingDir  The working directory.
             * \param   rtextPath   Full pathname of the RText file.
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
             * \brief   Gets or sets a unique process unique key based on the location of the RTxet file and the associated extensions
             *
             * \return  The process unique key.
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
             * \brief   Gets or sets the extension for which a back-end process was started.
             *
             * \return  The extension.
             */
            public string Extension { get; set; }
        };
        #endregion
        
        #region [Events]
        /**
         *
         * \brief   Delegate for the CommandCompleted event.
         *
         *
         * \param   source object of the event.
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
         *
         * \brief   Constructor.
         *
         *
         * \param   rTextFilePath       Full pathname of the .rtext file.
         * \param   workingDirectory    Pathname of the working directory.
         * \param   commandLine         The command line.
         * \param   processKey          The process key.
         */
        internal RTextBackendProcess(string rTextFilePath, string workingDirectory, string commandLine, string processKey, string extenstion)
        {
            _pInfo = new ProcessInfo(workingDirectory, rTextFilePath, commandLine, processKey);
            _timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                //wait one second before loading _odel in case of thousands of events / sec
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _timer.Tick += OnTimerElapsed;
            _extension = extenstion;
        }

        /**
         * \brief   Constructor.
         *
         * \param   rTextFilePath   Full pathname of the text file.
         * \param   ext             The extension of the RText file. 
         * \remark  For each extension, even of the same .rtext file, a different process should be started.
         * \param   settings        Allows access to persistent settings.
         */
        internal RTextBackendProcess(string rTextFilePath, string ext, ISettings settings)
            : this(rTextFilePath, Path.GetDirectoryName(rTextFilePath), GetCommandLine(rTextFilePath, ext), rTextFilePath + ext, ext)
        {
            _settings                      = settings;
            _connector                     = new Connector(this);
            _workspaceFileWatcherDebouncer = new VoidDelayedEventHandler(new Action(RestartProcess), 1000);
        }

        internal async void Disconnect()
        {
            //send shutdown command
            _isShutingDown = true;
            await _connector.ExecuteAsync(new ShutdownRequest { command = Constants.Commands.STOP, invocation_id = -1 }, Constants.SYNCHRONOUS_COMMANDS_TIMEOUT, StateEngine.Command.Disconnected);
        }

        /**
         * Gets the process unique key.
         *
         * \return  The process unique key. The process key is the complete filepath of the .rtext file that started this process with the addition of the file extensions that are associated with this process e.g. .atm. This way
         *          It is guaranteed that the key of a process is a unique identifier.
         */
        internal string ProcKey
        {
            get
            {
                return _pInfo.ProcKey;
            }
        }

        internal void OnFileSaved(string file)
        {
            OnWorkspaceModified(file);
        }
        
        /**
         * Gets the port.
         *
         * \return  The port. This port is being reported by the back-end process upon startup, and it is used by the fronted to establish communication with the back-end via sockets.
         */
        internal int Port
        {
            get
            {
                return _pInfo.Port;
            }
        }

        internal string Workspace
        {
            get
            {
                return (_pInfo != null ? _pInfo.ProcKey : null);
            }
        }

        internal async Task<bool> InitializeBackendAsync()
        {
            //process never started, or process has exited or port could not be retrieved
            if (_process == null || _process.HasExited || _pInfo.Port == -1)
            {
                var task = CreateNewProcessAsync();
                if (await Task.WhenAny(task, Task.Delay(Constants.INITIAL_RESPONSE_TIMEOUT)) != task)
                {
                    // task timed out
                    CleanupProcess();
                    return false;
                }
                else
                {
                    //port retrieved
                    return _pInfo.Port != -1;
                }
            }
            else
            {
                return true;
            }
        }        
        
        internal Connector Connector { get { return _connector; } }
        
        /**
         *
         * \brief   Kills this process.
         *
         */
        internal void Kill()
        {
            CleanupProcess();
        }
        
        internal bool HasExited
        {
            get
            {
                if (_process != null)
                {
                    return _process.HasExited;
                }
                else
                {
                    return true;
                }
            }
        }
        #endregion
        
        #region [Helpers]

        private async Task CreateNewProcessAsync()
        {
            OnProcessExited(null, EventArgs.Empty);
            try
            {
                RetrieveCommandLine();
                //process was never started or has already been started and stopped
                Match aMatch = Regex.Match(_pInfo.CommandLine, @"(^\s*\S+)(.*)", RegexOptions.Compiled);
                System.Diagnostics.ProcessStartInfo aProcessStartInfo = new ProcessStartInfo(aMatch.Groups[1].Value, aMatch.Groups[2].Value);
                _pInfo.Name = aMatch.Groups[1].Value;
                aProcessStartInfo.CreateNoWindow = true;
                aProcessStartInfo.RedirectStandardError = true;
                aProcessStartInfo.RedirectStandardOutput = true;
                aProcessStartInfo.UseShellExecute = false;
                aProcessStartInfo.WorkingDirectory = _pInfo.WorkingDirectory;
                _process = new System.Diagnostics.Process();
                _process.StartInfo = aProcessStartInfo;
                //add filewatcher for .rtext file and all associated extensions
                string aExtensions = _pInfo.ProcKey.Substring(_pInfo.RTextFilePath.Length, _pInfo.ProcKey.Length - _pInfo.RTextFilePath.Length);
                Regex regexObj = new Regex(@"\.\w+");
                Match matchResults = regexObj.Match(aExtensions);
                string aExtensionsFilter = String.Empty;
                while (matchResults.Success)
                {
                    aExtensionsFilter += "*" + matchResults.Value + ";";
                    matchResults = matchResults.NextMatch();
                }
                aExtensionsFilter = aExtensionsFilter.Substring(0, aExtensionsFilter.Length - 1);
                _fileSystemWatcher = new Windows.Clr.FileWatcher(System.IO.Path.GetDirectoryName(_pInfo.RTextFilePath),
                                                                 (uint)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime),
                                                                  true,
                                                                  aExtensionsFilter,
                                                                  String.Empty,
                                                                  true,
                                                                  Windows.Clr.FileWatcherBase.STANDARD_BUFFER_SIZE);
                _fileSystemWatcher.Changed += OnRTextFileCreatedOrDeletedOrModified;
                _fileSystemWatcher.Deleted += OnRTextFileCreatedOrDeletedOrModified;
                _fileSystemWatcher.Created += OnRTextFileCreatedOrDeletedOrModified;
                _fileSystemWatcher.Renamed += OnRTextFileRenamed;
                _fileSystemWatcher.Error += ProcessError;
                //finally add .rtext watcher
                _workspaceSystemWatcher = new Windows.Clr.FileWatcher(System.IO.Path.GetDirectoryName(_pInfo.RTextFilePath),
                                                                      (uint)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime),
                                                                      false,
                                                                      "*" + Constants.WORKSPACE_TYPE,
                                                                      String.Empty,
                                                                      true,
                                                                      Windows.Clr.FileWatcherBase.STANDARD_BUFFER_SIZE);
                _workspaceSystemWatcher.Changed += OnWorkspaceDefinitionFileCreatedOrDeletedOrModified;
                _workspaceSystemWatcher.Deleted += OnWorkspaceDefinitionFileCreatedOrDeletedOrModified;
                _workspaceSystemWatcher.Created += OnWorkspaceDefinitionFileCreatedOrDeletedOrModified;
                _workspaceSystemWatcher.Renamed += OnWorkspaceDefinitionFileRenamed;
                _workspaceSystemWatcher.Error += ProcessError;
                _process.Exited += OnProcessExited;
                //disable dos-key or whatever actions are associated with cmd.exe
                _autoRunKey = DisableCmdExeCustomization();
                _process.EnableRaisingEvents = true;
                _process.Start();
                //start reading asynchronously with tasks
                _cancellationSource = new CancellationTokenSource();
                _stdOutReaderTask = new Task(() => ReadStream(_process.StandardOutput, _cancellationSource.Token), _cancellationSource.Token);
                _stdErrReaderTask = Task.Factory.StartNew(() => ReadStream(_process.StandardError, _cancellationSource.Token), _cancellationSource.Token);
                _pInfo.Port = -1; //initialize port every time this function is called
                var aPortTask = new Task<bool>(() =>
                {
                    var t = new CancelableTask<int>(new ActionWrapper<int, System.IO.StreamReader>(GetPortNumber, _process.StandardOutput), Constants.INITIAL_RESPONSE_TIMEOUT);
                    t.Execute();
                    if (!t.IsCancelled)
                    {
                        //port could be retrieved - back-end is running
                        if (t.Result != -1)
                        {
                            _pInfo.Port = t.Result;
                            //load model if specified option is enabled
                            if (_settings.Get<bool>(Settings.RTextNppSettings.AutoLoadWorkspace))
                            {
                                OnTimerElapsed(null, EventArgs.Empty);
                            }
                        }
                    }
                    WriteAutoRunValue(_autoRunKey);
                    _autoRunKey = String.Empty;
                    return t.Result != -1;
                });

                aPortTask.Start();
                bool aIsRtextServiceRunning = await aPortTask;
                if (aIsRtextServiceRunning)
                {
                    _stdOutReaderTask.Start();
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, _pInfo.ProcKey, "Exception caught while trying to start RText service : {0}", ex.Message);
                OnProcessExited(null, EventArgs.Empty);
            }
        }

        private int GetPortNumber(System.IO.StreamReader stream)
        {
            try
            {
                while (true)
                {
                    System.Threading.Thread.Sleep(Constants.OUTPUT_POLL_PERIOD);
                    if (_process.HasExited)
                    {
                        while (!stream.EndOfStream)
                        {
                            Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, _pInfo.ProcKey, stream.ReadLine());
                        }
                        return -1;
                    }
                    if (!stream.EndOfStream)
                    {
                        var aLine = stream.ReadLine();
                        Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, _pInfo.ProcKey, aLine);
                        if (_backendInitResponseRegex.IsMatch(aLine))
                        {
                            return Int32.Parse(_backendInitResponseRegex.Match(aLine).Groups[1].Value);
                        }
                    }
                }
            }
            catch
            {
                //process died while trying to retrieve port number
                return -1;
            }
        }

        
        /**
         *
         * \brief   Reads a synchronous stream asynchronously. .NET bug workaround.
         *
         * \param   stream  The stream.
         * \param   token   The cancellation token.
         */
        private void ReadStream(System.IO.StreamReader stream, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(Constants.OUTPUT_POLL_PERIOD);
                if (!stream.EndOfStream)
                {
                    string aLine = stream.ReadLine();
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, _pInfo.ProcKey, aLine);
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
            if (_process != null)
            {
                if (_isShutingDown)
                {
                    _timer.Stop();
                }
                CleanupProcess();
                if (ProcessExitedEvent != null)
                {
                    ProcessExitedEvent(this, new ProcessExitedEventArgs(_pInfo.ProcKey));
                }
            }
        }
        
        /**
         *
         * \brief   Restart process. Occurs when .rtext file is _odified.
         *
         */
        private async void RestartProcess()
        {
            try
            {
                OnProcessExited(null, EventArgs.Empty);
                await InitializeBackendAsync();
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, _pInfo.ProcKey, "Process.RestartProcess : Exception : {0}", ex.Message);
                //clean up
                CleanupProcess();
                OnProcessExited(null, EventArgs.Empty);
            }
        }
        
        private void RetrieveCommandLine()
        {
            if (File.Exists(_pInfo.RTextFilePath))
            {
                string cmdLine = GetCommandLine(_pInfo.RTextFilePath, _extension);
                if (cmdLine != null)
                {
                    _pInfo = new ProcessInfo(_pInfo.WorkingDirectory, _pInfo.RTextFilePath, cmdLine, _pInfo.ProcKey);
                }
                else
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, _pInfo.ProcKey, "Could not read command line for extension {1} from file : {0} after modifications were made to the file.", _pInfo.RTextFilePath, _extension);
                    return;
                }
            }
            else
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.FatalError, _pInfo.ProcKey, "Could not locate file : {0} after modifications were made to the file.", _pInfo.RTextFilePath);
                return;
            }
        }

        /**
         * Disables the command executable customization because some broken programs throw exceptions when one tries to start a program from the command line.
         *
         * \return  A string containing the cmd.exe auto run customization command.
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
                using (var fileStream = new FileStream(rtextFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var textReader = new StreamReader(fileStream))
                {
                    var content = textReader.ReadToEnd();
                    using (var i = content.SplitString(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).GetEnumerator())
                    {
                        while (i.MoveNext())
                        {
                            //find endings
                            Match matchResults = FileUtilities.FileExtensionRegex.Match(i.Current);
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
                            //OK found matching extension in .rtext file, check for command line - next line should be the command line
                            if (aHasFoundMatch && i.MoveNext() && !String.IsNullOrEmpty(i.Current))
                            {
                                return i.Current;
                            }
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
        private void OnWorkspaceDefinitionFileRenamed(object sender, System.IO.RenamedEventArgs e)
        {
            //.rtext should never be renamed - process must be restarted - which will result to an error
            _workspaceFileWatcherDebouncer.TriggerHandler();
        }

        private void OnWorkspaceDefinitionFileCreatedOrDeletedOrModified(object sender, System.IO.FileSystemEventArgs e)
        {
            //.rtext should never be recreated - process must be restarted
            _workspaceFileWatcherDebouncer.TriggerHandler();
        }
        
        /**
         *
         * \brief   Process errors of FileSystemWatcher objects. Re-initialize watchers.
         *
         * \param   sender  Source of the event.
         * \param   e       Error event information.
         */
        private void ProcessError(object sender, System.IO.ErrorEventArgs e)
        {
            if (ReferenceEquals(sender, _fileSystemWatcher))
            {
                //automate watcher should be restarted automatically
                Logging.Logger.Instance.Append(String.Format("File system watcher for automate files reported an error : {0}", e.GetException().Message));
            }
            else
            {
                //.rtext watcher will restart automatically
                Logging.Logger.Instance.Append(String.Format("File system watcher back-end root file reported an error : {0}", e.GetException().Message));
            }
        }
        
        /**
         *
         * \brief   Handle file renaming events.
         *
         * \param   sender  Source of the event.
         * \param   e       Renamed event information.
         */
        private void OnRTextFileRenamed(object sender, System.IO.RenamedEventArgs e)
        {
            OnWorkspaceModified(e.OldFullPath);
        }
        
        /**
         *
         * \brief   Handle file saved events.
         *
         *
         * \param   sender  Source of the event.
         * \param   e       File system event information.
         * \todo    Automatically save workspace before reloading the back-end.
         */
        private void OnRTextFileCreatedOrDeletedOrModified(object sender, System.IO.FileSystemEventArgs e)
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
            if (_settings.Get<bool>(Settings.RTextNppSettings.AutoSaveFiles))
            {
                //find .rtext file of this document
                string aRTextFilePath = FileUtilities.FindWorkspaceRoot(pathOfModifiedFile);
                Plugin.Instance.FileObserver.SaveWorkspaceFiles(aRTextFilePath);
            }
            if (_connector != null)
            {
                 _timer.Start();
            }
        }
        
        /**
         *
         * \brief   Event handler. Called when the dispatcher timer expires.
         *
         * \param   sender  Source of the event.
         * \param   e       Event information.
         */
        private async void OnTimerElapsed(object sender, EventArgs e)
        {
            //check needed so that the interval timer don't stop if a command could not be loaded - this way we can ensure that the complete model will always be loaded!
            if (_connector.CurrentState.State == RText.StateEngine.ConnectorStates.Loading)
            {
                if (!_isMessageDisplayed)
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, _pInfo.ProcKey, "Changes were made to automate files while the model was being loaded. New loading pending...", _pInfo.ProcKey);
                    _isMessageDisplayed = true;
                }
                return;
            }
            else
            {
                await _connector.BeginExecute(Connector.LOAD_COMMAND, RText.StateEngine.Command.LoadModel);
                _timer.Stop();
                _isMessageDisplayed = false;
            }
        }

        /**
         *
         * \brief   Cleanup process.
         * \todo    Send shutdown command instead of killing the process.
         */
        private void CleanupProcess()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    //clean up process here
                    _process.EnableRaisingEvents = false;
                    Utilities.ProcessUtilities.KillAllProcessesSpawnedBy(_process.Id);
                }
                _cancellationSource.Cancel();
                if (!(_stdErrReaderTask.IsCanceled || _stdErrReaderTask.IsCompleted || _stdErrReaderTask.IsFaulted))
                {
                    _stdErrReaderTask.Wait();
                }
                if (!(_stdOutReaderTask.IsCanceled || _stdOutReaderTask.IsCompleted || _stdOutReaderTask.IsFaulted))
                {
                    _stdOutReaderTask.Wait();
                }
            }
            catch (OperationCanceledException ex)
            {
                System.Diagnostics.Trace.WriteLine(String.Format("CleanupProcess : Read stream task aborted : {0}", ex.Message));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            finally
            {
                try
                {
                    if (_fileSystemWatcher != null)
                    {
                        _fileSystemWatcher.Changed -= OnRTextFileCreatedOrDeletedOrModified;
                        _fileSystemWatcher.Deleted -= OnRTextFileCreatedOrDeletedOrModified;
                        _fileSystemWatcher.Created -= OnRTextFileCreatedOrDeletedOrModified;
                        _fileSystemWatcher.Renamed -= OnRTextFileRenamed;
                        _fileSystemWatcher.Error -= ProcessError;
                        _fileSystemWatcher = null;
                    }
                    if (_workspaceSystemWatcher != null)
                    {
                        _workspaceSystemWatcher.Changed -= OnWorkspaceDefinitionFileCreatedOrDeletedOrModified;
                        _workspaceSystemWatcher.Created -= OnWorkspaceDefinitionFileCreatedOrDeletedOrModified;
                        _workspaceSystemWatcher.Deleted -= OnWorkspaceDefinitionFileCreatedOrDeletedOrModified;
                        _workspaceSystemWatcher.Error -= ProcessError;
                        _workspaceSystemWatcher.Renamed -= OnWorkspaceDefinitionFileRenamed;
                        _workspaceSystemWatcher = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(String.Format("Exception : {0}", ex.Message));
                }
                if (_process != null)
                {
                    _process.Exited -= OnProcessExited;
                    _process.Dispose();
                    _process = null;
                }
            }
        }
        #endregion
    }
}