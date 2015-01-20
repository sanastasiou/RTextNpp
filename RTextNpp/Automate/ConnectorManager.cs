using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using RTextNppPlugin.RTextEditor.Utilities;
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
        public void createConnector(string file)
        {
            //check if file extension is an automate file
            if (isAutomateFile(file))
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
                        _processList.Add(processKey, new RTextEditor.Process(rTextFileLocation, Path.GetExtension(file)));
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
        public void initialize(NppData nppData)
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
        #endregion

        #region Data Members
        private Dictionary<string, RTextEditor.Process> _processList = new Dictionary<string, RTextEditor.Process>();
        private NppData _nppData;                            //!< Access to notepad++ data.
        private static volatile ConnectorManager _instance;  //!< Singleton Instance.
        private static object _lock = new Object();          //!< Mutex.
        #endregion

        #region Implementation Details

        /**
         * \brief   Query if 'file' is an automate file.
         *
         * \param   file    The file.
         *
         * \return  true if file parameter is an automate file, false if not.
         * \todo    Add excluded file extensions from options.         
         */
        private bool isAutomateFile(string file)
        {
            try
            {
                string fileExt = Path.GetExtension(file);
                if (fileExt.StartsWith("."))
                {
                    fileExt = fileExt.Remove(0, 1);
                }

                //get npp configuration directory
                //get list of supported extensions
                string configDir = getNppConfigDirectory();
                if (!String.IsNullOrEmpty(configDir))
                {
                    //try to open external lexer configuration file
                    XDocument xmlDom = XDocument.Load(configDir + @"\" + Constants.EX_LEXER_CONFIG_FILENAME);
                    if (fileExt.Equals((xmlDom.Root.Element("Languages").Element("Language").Attribute("ext").Value), StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                    //check user defined extensions as well
                    string additionalExt = xmlDom.Root.Element("LexerStyles").Element("LexerType").Attribute("ext").Value;
                    if (!String.IsNullOrWhiteSpace(additionalExt))
                    {
                        foreach (var ext in additionalExt.Split(' '))
                        {
                            if (fileExt.Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }

                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }

        }

        /**
         * \brief   Gets npp configuration directory.
         *
         * \return  The npp configuration directory.
         */
        private string getNppConfigDirectory()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(_nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            string configDir = sbIniFilePath.ToString();

            try
            {
                if (Directory.Exists(configDir))
                {
                    return configDir;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /**
         * Constructor that prevents a default instance of this class from being created.
         */
        private ConnectorManager()
        {
        }
        #endregion
    }
}
