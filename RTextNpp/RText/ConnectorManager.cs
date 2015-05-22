using System;
using System.Collections.Generic;
using System.IO;
using RTextNppPlugin.Utilities;

namespace RTextNppPlugin.RText
{
    /**
     * \brief   Manager for connectors.
     *          Creates and destroyes Connector instances based on actual automate workspaces.
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

        internal ConnectorManager()
        {
             _processList = new Dictionary<string, Utilities.RTextBackendProcess>();
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
        internal void CreateConnector(string file)
        {
            //check if file extension is an automate file
            if (FileUtilities.IsAutomateFile(file))
            {
                //identify .rtext file
                string rTextFileLocation = FileUtilities.FindWorkspaceRoot(file);
                if (String.IsNullOrEmpty(rTextFileLocation))
                {
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "Could not find .rtext file for automate file {0}", file);
                }
                else
                {
                    string processKey = rTextFileLocation + Path.GetExtension(file);
                    Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, processKey, "Workspace root for file : {0} is : {1}", file, rTextFileLocation);

                    //maybe process already exists..
                    if (_processList.ContainsKey(processKey))
                    {
                        if (_processList[processKey].HasExited)
                        {
                            _processList[processKey].StartRTextService();
                        }
                    }
                    else
                    {
                        _processList.Add(processKey, new Utilities.RTextBackendProcess(rTextFileLocation, Path.GetExtension(file)));
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
                if (FileUtilities.IsAutomateFile(aCurrentFile))
                {
                    //find root of file
                    string aProcKey = FileUtilities.FindWorkspaceRoot(aCurrentFile) + Path.GetExtension(aCurrentFile);
                    if(_processList.ContainsKey(aProcKey) && !_processList[aProcKey].HasExited)
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
        private Dictionary<string, Utilities.RTextBackendProcess> _processList;
        private NppData _nppData;                            
        #endregion     
    }
}
