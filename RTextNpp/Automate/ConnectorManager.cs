using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using RTextNppPlugin.Utilities;
using System.IO;

namespace RTextNppPlugin.Automate
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
        public delegate void ConnectorAddedEvent(object source, ConnectorAddedEventArgs e);

        public event ConnectorAddedEvent OnConnectorAdded;  //!< Event queue for all listeners interested in OnConnectorAdded events.

        /**
         * Additional information for connector added events.
         */
        public class ConnectorAddedEventArgs : EventArgs
        {
            public String Workspace { get; private set; }

            public ConnectorAddedEventArgs(string workspace)
            {
                Workspace = workspace;
            }
        }

        #endregion

        #region Interface

        /**
         * \brief   Creates a connector.
         *
         * \param   file    The file.
         */
        public void CreateConnector(string file)
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

                    if (OnConnectorAdded != null)
                    {
                        OnConnectorAdded(this, new ConnectorAddedEventArgs(processKey));
                    }
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
                        _processList.Add(processKey, new Utilities.Process(rTextFileLocation, Path.GetExtension(file)));
                    }
                }
            }
        }

        /**
         * Initializes this ConnectorManager.
         *
         * \param   nppData Information describing the npp.
         * \remarks Must be called upon plugin initialization.                  
         */
        public void Initialize(NppData nppData)
        {
            _nppData = nppData;
        }

        /**
         * Gets the instance.
         *
         * \return  The instance.
         */
        public static ConnectorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ConnectorManager();
                    }
                }

                return _instance;
            }
        }

        /**
         * \brief   Gets the connector of the file which is being viewed.
         *
         * \return  The connector.
         */
        public Connector Connector
        {
            get
            {
                string aCurrentFile = CSScriptIntellisense.Npp.GetCurrentFile();
                if (FileUtilities.IsAutomateFile(aCurrentFile))
                {
                    //find root of file
                    string aProcKey = FileUtilities.FindWorkspaceRoot(aCurrentFile);
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
        private Dictionary<string, Utilities.Process> _processList = new Dictionary<string, Utilities.Process>();
        private NppData _nppData;                            //!< Access to notepad++ data.
        private static volatile ConnectorManager _instance;  //!< Singleton Instance.
        private static object _lock = new Object();          //!< Mutex.
        #endregion

        #region Implementation Details        

        /**
         * Constructor that prevents a default instance of this class from being created.
         */
        private ConnectorManager()
        {
        }
        #endregion
    }
}
