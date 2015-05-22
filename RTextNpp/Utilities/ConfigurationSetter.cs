using System;
using System.Configuration;
using System.Reflection;

namespace RTextNppPlugin.Utilities
{
    /**
     * A configuration setter. This class handles setting and reading configuration settings
     * from the application's .dll configuration file.
     */
    internal class ConfigurationSetter
    {
        #region [Data Members]
        INpp _nppHelper = null;
        private static object _lock = new Object();  //!< Mutex.

        #endregion

        internal ConfigurationSetter(INpp pluginHelper)
        {
            _nppHelper = pluginHelper;
        }

        internal void saveSetting<T>(T setting, string settingKey)
        {
            lock (_lock)
            {
                EnsureConfigurationFileExists();
                Configuration myDllConfig = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
                myDllConfig.AppSettings.Settings[settingKey].Value = setting.ToString();
                myDllConfig.Save();
            }
        }

        internal void readSetting<T>(ref T setting, string settingKey)
        {
            Configuration myDllConfig = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            setting = (T)Convert.ChangeType(myDllConfig.AppSettings.Settings[settingKey].Value, typeof(T));
        }

        private void EnsureConfigurationFileExists()
        {
            var configDir = _nppHelper.GetConfigDir();
            var configPath = configDir + "\\\\" + Assembly.GetExecutingAssembly().GetName().Name + ".config";
        }
    }
}
