using System;
using System.IO;
using System.Reflection;
using System.Xml;
using RTextNppPlugin.Logging;

namespace RTextNppPlugin.Utilities.Settings
{
    /**
     * A configuration setter. This class handles setting and reading configuration settings
     * from the application's .dll configuration file.
     */
    internal class ConfigurationSetter
    {
        #region [Data Members]
        INpp _nppHelper                               = null;          //!< Access to npp.
        private static object _lock                   = new Object();  //!< Mutex for conqurrent write access.
        private readonly XmlDocument DEFAULT_SETTINGS = null;          //!< Default settings doc.

        #endregion

        internal ConfigurationSetter(INpp pluginHelper)
        {
            _nppHelper = pluginHelper;
            DEFAULT_SETTINGS = new XmlDocument();
            DEFAULT_SETTINGS.LoadXml(Properties.Resources.RTextNpp_dll);
        }

        internal void saveSetting<T>(T setting, Settings.RTextNppSettings settingKey)
        {
            try
            {
                string aConfigPath = GetConfigurationPath();
                lock (_lock)
                {
                    EnsureConfigurationFileExists(aConfigPath);
                }
                XmlDocument aDoc = new XmlDocument();
                aDoc.Load(aConfigPath);
                foreach (XmlNode n in aDoc.DocumentElement.FirstChild.ChildNodes)
                {
                    if (n.Attributes["key"].Value.Equals(settingKey.ToString()))
                    {

                        n.Attributes["value"].Value = setting.ToString();
                        lock (_lock)
                        {
                            aDoc.Save(aConfigPath);
                        }

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Append(Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "internal void saveSetting<T>(T setting, Settings.RTextNppSettings settingKey) : Exception : {0}", ex.Message);
            }
        }

        internal void readSetting<T>(ref T setting, Settings.RTextNppSettings settingKey)
        {
            try
            {
                string aConfigPath = GetConfigurationPath();
                lock (_lock)
                {
                    EnsureConfigurationFileExists(aConfigPath);
                }
                XmlDocument aDoc = new XmlDocument();
                aDoc.Load(aConfigPath);
                foreach (XmlNode n in aDoc.DocumentElement.FirstChild.ChildNodes)
                {
                    if (n.Attributes["key"].Value.Equals(settingKey.ToString()))
                    {
                        setting = (T)Convert.ChangeType(n.Attributes["value"].Value, typeof(T));
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Append(Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "internal void saveSetting<T>(T setting, Settings.RTextNppSettings settingKey) : Exception : {0}", ex.Message);
            }
        }

        private void EnsureConfigurationFileExists(string configPath)
        {
            if (!File.Exists(configPath))
            {
                //create file with default settings
                DEFAULT_SETTINGS.Save(configPath);
            }
            else
            {
                //check if file has all required settings, else add missing settings
                EnsureSettingsAvailability();
            }
        }

        private string GetConfigurationPath()
        {
            var configDir = _nppHelper.GetConfigDir();
            return configDir + "\\" + Assembly.GetExecutingAssembly().GetName().Name + ".dll.config";
        }

        private void EnsureSettingsAvailability()
        {
            XmlDocument aDoc   = new XmlDocument();
            string aConfigPath = GetConfigurationPath();
            aDoc.Load(GetConfigurationPath());

            foreach(var s in Enum.GetValues(typeof(Settings.RTextNppSettings)))
            {
                bool hasSetting = false;
                foreach(XmlNode kvPair in aDoc.DocumentElement.FirstChild.ChildNodes)
                {
                    if(kvPair.Attributes["key"].Value.Equals(s.ToString()))
                    {
                        hasSetting = true;
                        break;
                    }
                }
                if(!hasSetting)
                {
                    var aMissingSetting = DEFAULT_SETTINGS.DocumentElement.FirstChild.ChildNodes[(int)s];
                    aMissingSetting = aDoc.DocumentElement.FirstChild.OwnerDocument.ImportNode(aMissingSetting, true);
                    aDoc.DocumentElement.FirstChild.AppendChild(aMissingSetting);
                    aDoc.Save(aConfigPath);
                }
            }
        }
    }
}
