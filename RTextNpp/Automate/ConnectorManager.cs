﻿using System;
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
        #region Interface
        public ConnectorManager(NppData nppData)
        {
            _nppData = nppData;
        }


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
                Logging.Logger.Instance.Append(String.Format("File : {0} is an automate file!\n", file), Logging.Logger.MessageType.Warning);
                //identify .rtext file
                
            }
        }
        #endregion

        #region Data Members
        private List<RTextEditor.Connector> _connectorList = new List<RTextEditor.Connector>();
        private readonly NppData _nppData;

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
                string fileExt = FileUtilities.getExt(file);
                if(fileExt.StartsWith("."))
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
                // if config path doesn't exist, we create it
                if (Directory.Exists(configDir))
                {
                    return configDir;
                }
                return null;
            }
            catch(Exception)
            {
                return null;
            }
        }
        #endregion
    }
}