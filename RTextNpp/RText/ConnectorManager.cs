using System;
using System.Collections.Generic;
using System.IO;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.Logging;
namespace RTextNppPlugin.RText
{
    /**
     * \brief   Manager for connectors.
     *          Creates and destroyes Connector instances based on actual rtext workspaces.
     */
    internal sealed class ConnectorManager
    {
        #region Events
        /**
         * Connector added event.
         *
         * \param   source  Source for the evemt.
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
        internal ConnectorManager(ISettings settings, INpp nppHelper)
        {
            _settings    = settings;
            _processList = new Dictionary<string, RTextBackendProcess>();
            _nppHelper   = nppHelper;
        }
        internal void ReleaseConnectors()
        {
            var keys = _processList.Keys;
            foreach(var key in keys)
            {
                _processList[key].CleanupProcess();
            }
        }
        /**
         * \brief   Creates a connector.
         *
         * \param   file    The file.
         */
        internal async void CreateConnector(string file)
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
         * \remarks Must be called upon plugin initialization.
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
                string aCurrentFile = Npp.Instance.GetCurrentFile();
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
        #region Data Members
        private Dictionary<string, RTextBackendProcess> _processList;
        private NppData _nppData;
        private readonly ISettings _settings = null;
        private readonly INpp _nppHelper     = null;
        #endregion
    }
}