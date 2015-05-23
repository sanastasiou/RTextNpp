using System;
using System.Collections.Generic;
using RTextNppPlugin.Logging;
using RTextNppPlugin.Utilities;

namespace RTextNppPlugin.Utilities.Settings
{
    internal sealed class Settings : ISettings
    {
        #region [Data Members]
        private ConfigurationSetter _configSetter;
        #endregion

        #region Events

        public delegate void SettingChangedEvent(object source, SettingChangedEventArgs e);

        public event SettingChangedEvent OnSettingChanged;                                      //!< Event queue for all listeners interested in OnConnectorAdded events.

        public class SettingChangedEventArgs : EventArgs
        {
            public RTextNppSettings Setting { get; private set; }

            public SettingChangedEventArgs(RTextNppSettings setting)
            {
                Setting = setting;
            }
        }

        #endregion

        internal enum RTextNppSettings : int
        {
            ConsoleWindowActive,
            AutoLoadWorkspace,
            AutoSaveFiles,
            AutoChangeWorkspace,
            ExcludeExtensions
        }

        #region [Interface]

        internal Settings(INpp pluginHelper)
        {
            _configSetter = new ConfigurationSetter(pluginHelper);

        }

        public string Get(RTextNppSettings settingKey)
        {
            string setting = String.Empty;
            _configSetter.readSetting(ref setting, settingKey);
            return setting;
        }

        public T Get<T>(RTextNppSettings settingKey) where T : struct
        {
            T setting = new T();
            _configSetter.readSetting(ref setting, settingKey);
            return setting;
        }

        public void Set<T>(T setting, RTextNppSettings settingKey)
        {
            _configSetter.saveSetting(setting, settingKey);
            if (OnSettingChanged != null)
            {
                OnSettingChanged(this, new SettingChangedEventArgs(settingKey));
            }
        }
        #endregion
    }
}
