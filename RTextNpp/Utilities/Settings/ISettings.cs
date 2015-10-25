using System;
namespace RTextNppPlugin.Utilities.Settings
{
    internal interface ISettings
    {
        string Get(Settings.RTextNppSettings settingKey);
        T Get<T>(Settings.RTextNppSettings settingKey) where T : struct;
        event Settings.SettingChangedEvent OnSettingChanged;
        void Set<T>(T setting, Settings.RTextNppSettings settingKey);
    }
}