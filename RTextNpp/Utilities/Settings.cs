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
        private static volatile Settings _instance;  //!< Singleton Instance.
        private static object _lock = new Object();  //!< Mutex.
        private List<string> _settingKeys;           //!< List of all setting keys
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

        /**
         * Constructor that prevents a default instance of this class from being created.
         */
        private Settings()
        {
            _settingKeys = new List<string>(Enum.GetNames(typeof(RTextNppSettings)));

        }

        #endregion

        #region [Interface]
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new Settings();
                    }
                }
                return _instance;
            }
        }

        public string Get(RTextNppSettings settingKey)
        {
            string setting = String.Empty;
            try
            {
                ConfigurationSetter.readSetting(ref setting, _settingKeys[(int)settingKey]);
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
                ConfigurationSetter.readSetting(ref setting, _settingKeys[(int)settingKey]);
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
                ConfigurationSetter.saveSetting(setting, _settingKeys[(int)settingKey]);
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
