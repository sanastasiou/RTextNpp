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
        public static void saveSetting(bool setting, string settingKey)
        {
            try
            {
                Configuration myDllConfig = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
                myDllConfig.AppSettings.Settings[settingKey].Value = setting.ToString();
                myDllConfig.Save();
            }
            catch (Exception ex)
            {
                //todo save to some logger output...
            }            
        }

        public static void readSetting(ref bool setting, string settingKey)
        {
            try
            {
                Configuration myDllConfig = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
                setting = Boolean.Parse(myDllConfig.AppSettings.Settings[settingKey].Value);                
            }
            catch (Exception ex)
            {
                //todo save to some logger output...
            }
        }
    }
}
