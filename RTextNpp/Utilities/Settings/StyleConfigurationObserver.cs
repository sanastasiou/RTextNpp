﻿using RTextNppPlugin.Logging;
using RTextNppPlugin.Scintilla;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml.Linq;

namespace RTextNppPlugin.Utilities.Settings
{
    internal interface IWordsStyle
    {
        string Name { get; }
        int StyleId { get; }
        Color Foreground { get; set; }
        Color Background { get; set; }
        string FontName { get; set; }
        bool IsUnderlined { get; }
        bool IsBold { get; }
        bool IsItalic { get; }
        int FontSize { get; set; }
        int FontStyle { get; set; }
        int ExtractFontStyle();
    }
    internal interface IStyleConfigurationObserver
    {
        IWordsStyle GetStyle(Constants.StyleId styleId);

        event EventHandler OnSettingsChanged;
    }

    internal class StyleConfigurationObserver : IStyleConfigurationObserver, IDisposable
    {
        #region [Data Members]
        private Dictionary<Constants.StyleId, IWordsStyle> _styles = new Dictionary<Constants.StyleId, IWordsStyle>();
        private Windows.Clr.FileWatcher _settingsWatcher           = null;
        private object _objectLock                                 = new Object();
        private event EventHandler _onSettingsChanged;
        private const string STYLES_FILE                           = Constants.Scintilla.PLUGIN_NAME + ".xml";
        private readonly INpp _nppHelper                           = null;
        private enum FondStyle : int
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
        private class WordsStyle : IWordsStyle
        {
            private readonly string _name;
            private readonly int _styleId;
            private Color _foreground;
            private Color _background;
            private string _fontName;
            private readonly bool _isUnderlined;
            private readonly bool _isBold;
            private readonly bool _isItalic;
            private int _fontSize;
            private int _fontStyle;
            public WordsStyle(
                string name,
                int styleId,
                Color foreground,
                Color background,
                string fontName,
                bool isUnderlined,
                bool isBold,
                bool isItalic,
                int fontSize,
                int fontStyle
                )
            {
                _name         = name;
                _styleId      = styleId;
                _foreground   = foreground;
                _background   = background;
                _fontName     = fontName;
                _isUnderlined = isUnderlined;
                _isBold       = isBold;
                _isItalic     = isItalic;
                _fontSize     = fontSize;
                _fontStyle    = fontStyle;
            }

            public string Name
            {
                get { return _name; }
            }

            public int StyleId
            {
                get { return _styleId; }
            }

            public Color Foreground
            {
                get { return _foreground; }
                set { _foreground = value; }
            }

            public Color Background
            {
                get { return _background; }
                set { _background = value; }
            }
                             
            public string FontName
            {
                get { return _fontName; }
                set { _fontName = value; }
            }
            
            public bool IsUnderlined
            {
                get { return _isUnderlined; }
            }
            
            public bool IsBold
            {
                get { return _isBold; }
            }
            
            public bool IsItalic
            {
                get { return _isItalic; }
            }
            
            public int FontSize
            {
                get { return _fontSize; }
                set { _fontSize = value; }
            }

            public int FontStyle
            {
                get { return _fontStyle; }
                set { _fontStyle = value; }
            }

            public int ExtractFontStyle()
            {
                //convert font setting to scintilla font-style
                if(IsBold && IsItalic && IsUnderlined)
                {
                    return (int)FondStyle.FondStyle_Bold_Underline_Italic;
                }
                else if(IsBold && !IsUnderlined && !IsItalic)
                {
                    return (int)FondStyle.FondStyle_Bold;
                }
                else if(!IsBold && IsUnderlined && !IsItalic)
                {
                    return (int)FondStyle.FondStyle_Underline;
                }
                else if(!IsBold && !IsUnderlined && IsItalic)
                {
                    return (int)FondStyle.FondStyle_Italic;
                }
                else if(IsBold && IsUnderlined && !IsItalic)
                {
                    return (int)FondStyle.FondStyle_Bold_Underline;
                }
                else if(IsBold && !IsUnderlined && IsItalic)
                {
                    return (int)FondStyle.FondStyle_Bold_Italic;
                }
                else if(!IsBold && IsUnderlined && IsItalic)
                {
                    return (int)FondStyle.FondStyle_Italic_Underline;
                }
                else
                {
                    return 0;
                }
            }
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
            if(nppHelper == null)
            {
                throw new ArgumentNullException("nppHelper");
            }
            _nppHelper = nppHelper;
        }
        
        public void EnableStylesObservation()
        {
            string aConfigDir = _nppHelper.GetConfigDir();
            if (File.Exists(aConfigDir + "\\" + STYLES_FILE))
            {
                _settingsWatcher = new Windows.Clr.FileWatcher(aConfigDir,
                                                               (uint)(System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.CreationTime),
                                                               false,
                                                               "*.xml",
                                                               String.Empty,                                                               
                                                               false,
                                                               Windows.Clr.FileWatcherBase.STANDARD_BUFFER_SIZE);
                _settingsWatcher.Changed += OnRTextFileCreatedOrDeletedOrModified;
                _settingsWatcher.Deleted += OnRTextFileCreatedOrDeletedOrModified;
                _settingsWatcher.Created += OnRTextFileCreatedOrDeletedOrModified;
                _settingsWatcher.Error += ProcessError;
                LoadStyles();
                OnRTextFileCreatedOrDeletedOrModified(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, String.Empty, STYLES_FILE));
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
        
        public IWordsStyle GetStyle(Constants.StyleId styleId)
        {
            if (_styles.ContainsKey(styleId))
            {
                return _styles[styleId];
            }
            return default(IWordsStyle);
        }

        #endregion
        
        #region [Helpers]

        private Color ConvertRGBToColor(string rgbString)
        {
            int rgb = int.Parse(rgbString, System.Globalization.NumberStyles.AllowHexSpecifier);
            byte g  = (byte)((rgb >> 8) & 0xFF);
            byte r  = (byte)((rgb >> 16) & 0xFF);
            byte b  = (byte)(rgb & 0xFF);
            return new Color { R = r, G = g, B = b, A = 0xFF };
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
                    _settingsWatcher.Error   -= ProcessError;
                    _settingsWatcher.Dispose();
                    _settingsWatcher = null;
                }
            }
        }
        
        private void LoadStyles()
        {
            string aSettingsFile = _nppHelper.GetConfigDir() + "\\" + STYLES_FILE;
            if (File.Exists(aSettingsFile))
            {
                XDocument aColorFile = XDocument.Load(aSettingsFile);
                var aStyles = from wordStyles in aColorFile.Root.Descendants(Constants.Wordstyles.WORDSTYLES_ELEMENT_NAME) select wordStyles;
                foreach(var style in aStyles)
                {
                    bool aIsUnderlined = false;
                    bool aIsItalic     = false;
                    bool aIsBold       = false;
                    AnalyzeStyle((FondStyle)(int)style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_FONTSTYLE), ref aIsBold, ref aIsItalic, ref aIsUnderlined);

                    _styles[(Constants.StyleId)Int32.Parse(style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_STYLEID).Value)] = new WordsStyle(
                        style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_NAME).Value,
                        Int32.Parse(style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_STYLEID).Value),
                        ConvertRGBToColor((string)style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_FGCOLOR)),
                        ConvertRGBToColor((string)style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_BGCOLOR)),
                        style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_FONTNAME).Value,
                        aIsUnderlined,
                        aIsBold,
                        aIsItalic,
                        string.IsNullOrWhiteSpace(((string)style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_FONTSIZE))) ? 0 : (int)style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_FONTSIZE),
                        Int32.Parse(style.Attribute(Constants.Wordstyles.STYLE_ATTRIBUTE_FONTSTYLE).Value)
                        );
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
                LoadStyles();
                if (_onSettingsChanged != null)
                {
                    _onSettingsChanged(this, new EventArgs());
                }
            }
        }
        private void ProcessError(object sender, ErrorEventArgs e)
        {
            //restart filewatcher
            _settingsWatcher.Changed -= OnRTextFileCreatedOrDeletedOrModified;
            _settingsWatcher.Error   -= ProcessError;
            _settingsWatcher.Dispose();
            _settingsWatcher         = null;
            EnableStylesObservation();
            LoadStyles();
        }
        private void AdjustLeadingZeros(ref char [] rgbArray, int offset )
        {
            if (rgbArray[offset] == '0' && rgbArray[offset + 1] == '0')
            {
                rgbArray[offset + 1] = '1';
            }
        }
        #endregion
    }
}