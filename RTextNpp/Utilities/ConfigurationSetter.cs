using System;
using System.Configuration;
using System.Reflection;

namespace RTextNppPlugin.Utilities
{
    /**
     * A configuration setter. This class handles setting and reading configuration settings
     * from the application's .dll configuration file.
     */
    class ConfigurationSetter
    {
        public static void saveSetting<T>(T setting, string settingKey)
        {
            Configuration myDllConfig = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            myDllConfig.AppSettings.Settings[settingKey].Value = setting.ToString();
            myDllConfig.Save();
        }

        public static void readSetting<T>(ref T setting, string settingKey)
        {
            Configuration myDllConfig = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            setting = (T)Convert.ChangeType(myDllConfig.AppSettings.Settings[settingKey].Value, typeof(T));
        }
    }
}
