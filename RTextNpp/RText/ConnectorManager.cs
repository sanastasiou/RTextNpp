﻿using System;
using System.Collections.Generic;
using System.IO;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.Logging;
using RTextNppPlugin.Scintilla;

namespace RTextNppPlugin.RText
{
    /**
     * \brief   Manager for connectors.
     *          Creates and destroys Connector instances based on actual RText workspaces.
     */
    internal sealed class ConnectorManager : IDisposable
    {
        #region [Data Members]
        private Dictionary<string, RTextBackendProcess> _processList = null;
        private NppData _nppData                                     = default(NppData);
        private readonly ISettings _settings                         = null;
        private readonly INpp _nppHelper                             = null;
        private readonly Plugin _plugin                              = null;
        #endregion

        #region [Events]
        
        /**
         * Connector added event.
         *
         * \param   source  Source for the event.
         * \param   e       Connector added event information.
         */
        internal delegate void ConnectorAddedEvent(object source, ConnectorAddedEventArgs e);
        
        internal event ConnectorAddedEvent OnConnectorAdded;  //!< Event queue for all listeners interested in OnConnectorAdded events.
        
        /**
         * Additional information for connector added events.
         */
        internal class ConnectorAddedEventArgs : EventArgs
        {
            internal String Workspace { get; private set; }
            internal Connector Connector { get; private set; }
            internal ConnectorAddedEventArgs(string workspace, Connector connector)
            {
                Workspace = workspace;
                Connector = connector;
            }
        }
        
        #endregion
        
        #region Interface
        
        internal ConnectorManager(ISettings settings, INpp nppHelper, Plugin plugin)
        {
            _settings              = settings;
            _processList           = new Dictionary<string, RTextBackendProcess>();
            _nppHelper             = nppHelper;
            plugin.BufferActivated += OnBufferActivated;
        }

        internal void OnFileSaved(string file)
        {
            if (FileUtilities.IsRTextFile(file, _settings, _nppHelper))
            {
                string rTextFileLocation = FileUtilities.FindWorkspaceRoot(file);
                if (!string.IsNullOrEmpty(rTextFileLocation))
                {
                    string processKey = rTextFileLocation + Path.GetExtension(file);
                    if(_processList.ContainsKey(processKey))
                    {
                        _processList[processKey].OnFileSaved(file);
                    }
                }
            }
        }

        internal void ReleaseConnectors()
        {
            var keys = _processList.Keys;
            foreach(var key in keys)
            {
                _processList[key].Disconnect();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            _plugin.BufferActivated -= OnBufferActivated;
        }

        /**
         * \brief   Creates a connector.
         *
         * \param   file    The file.
         */
        private async void CreateConnector(string file)
        {
            //check if file extension is an automate file
            if (FileUtilities.IsRTextFile(file, _settings, _nppHelper))
            {
                //identify .rtext file
                string rTextFileLocation = FileUtilities.FindWorkspaceRoot(file);
                if (String.IsNullOrEmpty(rTextFileLocation))
                {
                    Logger.Instance.Append(Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "Could not find .rtext file for automate file {0}", file);
                }
                else
                {
                    string processKey = rTextFileLocation + Path.GetExtension(file);
                    Logger.Instance.Append(Logger.MessageType.Info, processKey, "Workspace root for file : {0} is : {1}", file, rTextFileLocation);
                    //maybe process already exists..
                    if (!_processList.ContainsKey(processKey))
                    {
                        _processList.Add(processKey, new RTextBackendProcess(rTextFileLocation, Path.GetExtension(file), _settings));
                    }
                    if (_processList[processKey].HasExited)
                    {
                        await _processList[processKey].InitializeBackendAsync();
                    }
                    if (OnConnectorAdded != null)
                    {
                        OnConnectorAdded(this, new ConnectorAddedEventArgs(processKey, _processList[processKey].Connector));
                    }
                }
            }
        }
        
        /**
         * Initializes this ConnectorManager.
         *
         * \param   nppData Information describing the Npp.Instance.
         * \remarks Must be called upon plug-in initialization.
         */
        internal void Initialize(NppData nppData)
        {
            _nppData = nppData;
        }
        
        /**
         * \brief   Gets the connector of the file which is being viewed.
         *
         * \return  The connector.
         */
        internal Connector Connector
        {
            get
            {
                string aCurrentFile = Npp.Instance.GetCurrentFilePath();
                if (FileUtilities.IsRTextFile(aCurrentFile, _settings, _nppHelper))
                {
                    //find root of file
                    string aProcKey = FileUtilities.FindWorkspaceRoot(aCurrentFile) + Path.GetExtension(aCurrentFile);
                    if (_processList.ContainsKey(aProcKey))
                    {
                        return _processList[aProcKey].Connector;
                    }
                    else
                    {
                        CreateConnector(aCurrentFile);
                    }
                }
                return null;
            }
        }
        
        #endregion
        
        #region [Helpers]
        
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose any disposable fields here
                GC.SuppressFinalize(this);
            }
        }
        
        #endregion

        #region [Event Handlers]
        
        void OnBufferActivated(object source, string file, View view)
        {
            CreateConnector(file);
        }
        
        #endregion
    }
}