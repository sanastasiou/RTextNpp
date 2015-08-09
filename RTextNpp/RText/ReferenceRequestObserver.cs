using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace RTextNppPlugin.RText
{
    using CSScriptIntellisense;
    using DllExport;
    using Parsing;
    using Utilities;
    using Utilities.Settings;
    using WpfControls;
    class ReferenceRequestObserver
    {
        #region [Data Members]
        private INpp _nppHelper                                       = null;                        //!< Interface to Npp message system.
        private ISettings _settings                                   = null;                        //!< Interface to RTextNpp settings.
        private MouseMonitor _mouseMovementObserver                   = new MouseMonitor();          //!< Low level mouse monitor hook.        
        private Tokenizer.TokenTag _previousReferenceToken            = default(Tokenizer.TokenTag); //!< Holds previous highlighted reference token.
        private bool _isKeyboardShortCutActive                        = false;                       //!< Indicates if reference show shortcut key is active.
        private bool _highLightToken                                  = false;                       //!< Whether a reference token is highlighted.
        private IWin32 _win32Helper                                   = null;                        //!< Handle to win32 helper instance.
        private ILinkTargetsWindow _refWindow                         = null;                        //!< Handle to reference window.
        private DelayedEventHandler _mouseMoveDebouncer               = null;                        //!< Debounces mose movement for a short period of time so that CPU is not taxed.
        #endregion

        #region [Events]

        #endregion

        #region [Interface]
        internal ReferenceRequestObserver(INpp nppHelper, ISettings settings, IWin32 win32helper, ILinkTargetsWindow refWindow)
        {
            _nppHelper                       = nppHelper;
            _settings                        = settings;
            _win32Helper                     = win32helper;
            _mouseMovementObserver.MouseMove += OnMouseMovementObserverMouseMove;
            _mouseMovementObserver.MouseClicked += OnMouseMovementObserverMouseClicked;
            IsKeyboardShortCutActive         = false;
            _refWindow                       = refWindow;
            _mouseMoveDebouncer              = new DelayedEventHandler(new ActionWrapper(DoMouseMovementObserverMouseMove), 100);
        }

        bool OnMouseMovementObserverMouseClicked(VisualUtilities.MouseMessages arg)
        {
            if (_highLightToken)
            {
                Plugin.OnHotSpotClicked();
                //return true to "eat" event
                return true;
            }
            return false;
        }

        internal bool IsKeyboardShortCutActive 
        { 
            set
            {
                if(value != _isKeyboardShortCutActive)
                {
                    _isKeyboardShortCutActive = value;
                    if(!_isKeyboardShortCutActive)
                    {
                        HideUnderlinedToken();
                    }
                    else
                    {
                        _refWindow.IssueReferenceLinkRequestCommand(Tokenizer.FindTokenUnderCursor(_nppHelper));                        
                    }
                    Enable(value);
                }
            }
            get
            {
                return _isKeyboardShortCutActive;
            }
        }

        public Tokenizer.TokenTag UnderlinedToken
        {
            get
            {
                return _previousReferenceToken;
            }
            set
            {
                if (!_previousReferenceToken.Equals(value))
                {
                    _previousReferenceToken = value;
                    HideUnderlinedToken();
                }
            }
        }

        internal void UnderlineToken()
        {
            int aReferenceColor = GetReferenceLinkColor();
            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle,   SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 1);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 1);

            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);

            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTACTIVEFORE, 1, aReferenceColor);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTACTIVEFORE, 1, aReferenceColor);
            _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_STARTSTYLING, _previousReferenceToken.BufferPosition, 0);
            _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_SETSTYLING, _previousReferenceToken.EndColumn - _previousReferenceToken.StartColumn, (int)_previousReferenceToken.Type);
            _highLightToken = true;
        }

        internal void HideUnderlinedToken()
        {
            if (_highLightToken)
            {
                _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 0);
                _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 0);
                _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_STARTSTYLING, _previousReferenceToken.BufferPosition, 0);
                _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_SETSTYLING, _previousReferenceToken.EndColumn - _previousReferenceToken.StartColumn, (int)_previousReferenceToken.Type);

                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X - 1, System.Windows.Forms.Cursor.Position.Y);
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X + 1, System.Windows.Forms.Cursor.Position.Y);
                _highLightToken = false;
            }
        }

        internal bool IsTokenUnderlined
        {
            get
            {
                return _highLightToken;
            }
        }
        #endregion

        #region [Helpers]

        private void Enable(bool enable)
        {
            if (enable && !_mouseMovementObserver.IsInstalled)
            {
                _mouseMovementObserver.Install();
            }
            else
            {
                _mouseMovementObserver.Uninstall();
                _mouseMoveDebouncer.Cancel();
            }
        }

        private int GetReferenceLinkColor()
        {
            string aPluginTokenizerConfigFile = _nppHelper.GetConfigDir() + "\\" + Constants.PluginName + ".xml";
            if (File.Exists(aPluginTokenizerConfigFile))
            {
                XDocument aColorFile = XDocument.Load(aPluginTokenizerConfigFile);

                var underLineTokenConfig = (from wordStyles in aColorFile.Root.Descendants("WordsStyle")
                                            where (string)wordStyles.Attribute("name") == Constants.REFERENCE_LINK_NAME
                                            select wordStyles).First();
                string aFgColor = (string)underLineTokenConfig.Attribute("fgColor");
                //need to prepare string in case 0x0000FF is present, information will be lost if we convert this to rgb since an int will just replace leading zeros
                aFgColor = aFgColor.Replace("00", "01");
                //for some reason scitnilla expects bgr instead of rgb, documentation is wrong
                int rgb = int.Parse(aFgColor, System.Globalization.NumberStyles.AllowHexSpecifier);
                int g = (rgb >> 8) & 0xFF;
                int r = (rgb >> 16) & 0xFF;
                int b = rgb & 0xFF;
                int bgr = b;
                bgr <<= 8;
                bgr |= g;
                bgr <<= 8;
                bgr |= r;
                return bgr;
            }
            //return blue
            return 0xFF0000;
        }

        private void OnMouseMovementObserverMouseMove()
        {
            _mouseMoveDebouncer.TriggerHandler();
        }

        private void DoMouseMovementObserverMouseMove()
        {
            if (_isKeyboardShortCutActive)
            {
                if (!_refWindow.IsMouseInsidedWindow())
                {                    
                    var aRefToken = Tokenizer.FindTokenUnderCursor(_nppHelper);
                    if (aRefToken.CanTokenHaveReference() && !aRefToken.Equals(_previousReferenceToken))
                    {
                        _refWindow.IssueReferenceLinkRequestCommand(aRefToken);
                    }
                    else if (!aRefToken.CanTokenHaveReference())
                    {
                        HideUnderlinedToken();
                        _refWindow.Hide();
                    }
                    else
                    {
                        //tokens are equal - issue command if underlining is not active
                        if (!_refWindow.IsVisible)
                        {
                            _refWindow.IssueReferenceLinkRequestCommand(aRefToken);
                        }
                    }
                }
            }
            else
            {
                HideUnderlinedToken();
            }
        }

        #endregion                
    }
}
