using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace RTextNppPlugin.RText
{
    using CSScriptIntellisense;
    using DllExport;
    using RText.Parsing;
    using RTextNppPlugin.WpfControls;
    using Utilities;
    using Utilities.Settings;
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
            IsKeyboardShortCutActive         = false;
            _refWindow                       = refWindow;
        }

        private void Enable(bool enable)
        {
            if(enable && !_mouseMovementObserver.IsInstalled)
            {
                _mouseMovementObserver.Install();
            }
            else
            {
                _mouseMovementObserver.Uninstall();
            }
        }

        private void CancelHighlighting()
        {
            _highLightToken = false;
            HideUnderlinedToken();
        }

        internal bool IsKeyboardShortCutActive 
        { 
            set
            {
                if(value != _isKeyboardShortCutActive)
                {
                    _isKeyboardShortCutActive = value;
                    Enable(value);
                    if(!_isKeyboardShortCutActive)
                    {
                        HideUnderlinedToken();
                    }
                    else
                    {
                        var aRefToken = Tokenizer.FindTokenUnderCursor(_nppHelper);
                        if (aRefToken.CanTokenHaveReference() && FileUtilities.IsRTextFile(_settings, _nppHelper))
                        {
                            _refWindow.IssueReferenceLinkRequestCommand(aRefToken);
                        }
                        else
                        {
                            CancelHighlighting();
                        }
                    }
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
                _previousReferenceToken = value;
            }
        }

        #endregion

        private void UnderlineToken()
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
        }

        private int GetReferenceLinkColor()
        {
            string aPluginTokenizerConfigFile = _nppHelper.GetConfigDir() + "\\" + Constants.PluginName + ".xml";
            if(File.Exists(aPluginTokenizerConfigFile))
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

            return 0x0000FF;
        }

        private void HideUnderlinedToken()
        {
            if (_highLightToken)
            {                                
                _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 0);
                _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 0);
                _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_STARTSTYLING, _previousReferenceToken.BufferPosition, 0);
                _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_SETSTYLING, _previousReferenceToken.EndColumn - _previousReferenceToken.StartColumn, (int)_previousReferenceToken.Type);
                _highLightToken = false;
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X - 1, System.Windows.Forms.Cursor.Position.Y);
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X + 1, System.Windows.Forms.Cursor.Position.Y);
            }
        }

        private void OnMouseMovementObserverMouseMove()
        {
            if (_isKeyboardShortCutActive && FileUtilities.IsRTextFile(_settings, _nppHelper))
            {
                var aRefToken = Tokenizer.FindTokenUnderCursor(_nppHelper);
                if (aRefToken.CanTokenHaveReference())
                {
                    _refWindow.IssueReferenceLinkRequestCommand(aRefToken);
                }
                else
                {
                    CancelHighlighting();
                }
            }
            else
            {
                CancelHighlighting();
            }
        }        
    }
}
