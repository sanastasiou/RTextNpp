using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.IO;
using RTextNppPlugin.Logging;

namespace RTextNppPlugin.Utilities.Settings
{
    internal interface IWordsStyle
    {
        Color ErrorOverviewBackground { get; }
        Color ErrorOverviewForeground { get; }
        int StyleId { get; }
        string FontName { get; }
        bool IsUnderlined { get; }
        bool IsBold { get; }
        bool IsItalic { get; }
        int FontSize { get; }
    }

    internal interface IStyleConfigurationObserver
    {
        IWordsStyle GetStyle(string styleName);

        event EventHandler OnSettingsChanged;
    }

    internal class StyleConfigurationObserver : IStyleConfigurationObserver, IDisposable
    {
        #region [Data Members]        
        private Dictionary<string, IWordsStyle> _styles                        = new Dictionary<string, IWordsStyle>();
        private FileSystemWactherCLRWrapper.FileSystemWatcher _settingsWatcher = null;
        private object _objectLock                                             = new Object();
        private event EventHandler _onSettingsChanged;
        private const string STYLES_FILE                                       = Constants.PluginName + ".xml";
        private readonly INpp _nppHelper                                       = null;

        

        enum FondStyle : int
        {
            FondStyle_Default,
            FondStyle_Bold,
            FondStyle_Italic,
            FondStyle_Bold_Italic,
            FondStyle_Underline,
            FondStyle_Bold_Underline,
            FondStyle_Italic_Underline,
            FondStyle_Bold_Underline_Italic
        }
        #endregion

        #region [Interface]
        
        event EventHandler IStyleConfigurationObserver.OnSettingsChanged
        {
            add
            {
                lock (_objectLock)
                {
                    _onSettingsChanged += value;
                }
            }
            remove
            {
                lock (_objectLock)
                {
                    _onSettingsChanged -= value;
                }
            }
        }

        public StyleConfigurationObserver(INpp nppHelper)
        {
            _nppHelper = nppHelper;            
        }

        public void EnableStylesObservation()
        {
            string aConfigDir = _nppHelper.GetConfigDir();
            if (File.Exists(aConfigDir + "\\" + STYLES_FILE))
            {
                _settingsWatcher = new FileSystemWactherCLRWrapper.FileSystemWatcher(aConfigDir,
                                                                                     false,
                                                                                     "*.xml",
                                                                                     String.Empty,
                                                                                     (uint)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime),
                                                                                     false
                                                                                     );
                _settingsWatcher.Changed += OnRTextFileCreatedOrDeletedOrModified;
                _settingsWatcher.Deleted += OnRTextFileCreatedOrDeletedOrModified;
                _settingsWatcher.Created += OnRTextFileCreatedOrDeletedOrModified;
                _settingsWatcher.Renamed += OnRTextFileRenamed;
                _settingsWatcher.Error += ProcessError;
            }
            else
            {
                Logger.Instance.Append(Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "{0} file doesn't exist. Automatic style update is disabled.", aConfigDir + "\\" + STYLES_FILE);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IWordsStyle GetStyle(string styleName)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region [Helpers]
        private T ReadSetting<T>(string styleName, string attributeName)
        {
            return default(T);
        }

        private Color ConvertRGBToColor(string RGB)
        {
            return default(Color);
        }

        private void AnalyzeStyle(FondStyle style, ref bool isBold, ref bool isItalic, ref bool isUnderlined)
        {
            switch (style)
            {
                case FondStyle.FondStyle_Bold:
                    isBold = true;
                    isItalic = isUnderlined = false;
                    break;
                case FondStyle.FondStyle_Italic:
                    isItalic = true;
                    isBold = isUnderlined = false;
                    break;
                case FondStyle.FondStyle_Bold_Italic:
                    isBold = isItalic = true;
                    isUnderlined = false;
                    break;
                case FondStyle.FondStyle_Underline:
                    isBold = isItalic = false;
                    isUnderlined = true;
                    break;
                case FondStyle.FondStyle_Bold_Underline:
                    isBold = isUnderlined = true;
                    isItalic = false;
                    break;
                case FondStyle.FondStyle_Italic_Underline:
                    isItalic = isUnderlined = true;
                    isBold = false;
                    break;
                case FondStyle.FondStyle_Bold_Underline_Italic:
                    isBold = isItalic = isUnderlined = true;
                    break;
                default:
                    isBold = isItalic = isUnderlined = false;
                    break;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (_settingsWatcher != null)
                {
                    _settingsWatcher.Changed -= OnRTextFileCreatedOrDeletedOrModified;
                    _settingsWatcher.Deleted -= OnRTextFileCreatedOrDeletedOrModified;
                    _settingsWatcher.Created -= OnRTextFileCreatedOrDeletedOrModified;
                    _settingsWatcher.Renamed -= OnRTextFileRenamed;
                    _settingsWatcher.Error   -= ProcessError;
                    _settingsWatcher.Dispose();
                    _settingsWatcher = null;
                }
            }
        }

        ~StyleConfigurationObserver()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        #endregion        

        #region [Event Handlers]
        private void OnRTextFileCreatedOrDeletedOrModified(object sender, FileSystemEventArgs e)
        {
            if (Path.GetFileName(e.FullPath) == STYLES_FILE)
            {
                Logger.Instance.Append(Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "Styles changed...");
                if (_onSettingsChanged != null)
                {
                    _onSettingsChanged(this, new EventArgs());
                }
            }
        }

        private void OnRTextFileRenamed(object sender, RenamedEventArgs e)
        {
            if (_onSettingsChanged != null)
            {
                _onSettingsChanged(this, new EventArgs());
            }
        }

        private void ProcessError(object sender, ErrorEventArgs e)
        {

        }
        #endregion
    }
}
