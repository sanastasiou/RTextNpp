using System;
using System.Collections.Generic;
using RTextNppPlugin.Logging;
using RTextNppPlugin.Utilities;

namespace RTextNppPlugin.Utilities
{
    internal sealed class Settings
    {
        #region Events

        /**
         * Connector added event.
         *
         * \param   source  Source for the evemt.
         * \param   e       Connector added event information.
         */
        public delegate void SettingChangedEvent(object source, SettingChangedEventArgs e);

        public event SettingChangedEvent OnSettingChanged;  //!< Event queue for all listeners interested in OnConnectorAdded events.

        /**
         * Additional information for connector added events.
         */
        public class SettingChangedEventArgs : EventArgs
        {
            public RTextNppSettings Setting { get; private set; }

            public SettingChangedEventArgs(RTextNppSettings setting)
            {
                Setting = setting;
            }
        }

        #endregion

        #region [Data Members]        
        private List<string> _settingKeys;           //!< List of all setting keys
        private ConfigurationSetter _configSetter;
        #endregion

        internal enum RTextNppSettings : int
        {
            ConsoleWindowActive,
            AutoLoadWorkspace,
            AutoSaveFiles,
            AutoChangeWorkspace,
            ExcludeExtensions
        }

        #region [Implementation Details]

        internal Settings(INpp pluginHelper)
        {
            _settingKeys  = new List<string>(Enum.GetNames(typeof(RTextNppSettings)));
            _configSetter = new ConfigurationSetter(pluginHelper);

        }

        #endregion

        #region [Interface]

        public string Get(RTextNppSettings settingKey)
        {
            string setting = String.Empty;
            try
            {
                _configSetter.readSetting(ref setting, _settingKeys[(int)settingKey]);
            }
            catch (Exception ex)
            {
                Logger.Instance.Append(Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "Could not read setting {0} - exception : {1}", settingKey, ex.Message);
            }
            return setting;
        }

        public T Get<T>(RTextNppSettings settingKey) where T : new()
        {
            T setting = new T();
            try
            {
                _configSetter.readSetting(ref setting, _settingKeys[(int)settingKey]);
            }
            catch(Exception ex)
            {
                Logger.Instance.Append(Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "Could not read setting {0} - exception : {1}", settingKey, ex.Message);
            }
            return setting;
        }

        public void Set<T>(T setting, RTextNppSettings settingKey)
        {
            try
            {
                _configSetter.saveSetting(setting, _settingKeys[(int)settingKey]);
                if (OnSettingChanged != null)
                {
                    OnSettingChanged(this, new SettingChangedEventArgs(settingKey));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Append(Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "Could not save setting {0} - exception : {1}", settingKey, ex.Message);
            }
        }
        #endregion


    }
}
