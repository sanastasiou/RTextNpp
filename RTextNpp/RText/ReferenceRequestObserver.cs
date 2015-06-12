﻿using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace RTextNppPlugin.RText
{
    using System;
    using CSScriptIntellisense;
    using DllExport;
    using RText.Parsing;
    using Utilities;
    using Utilities.Settings;
    class ReferenceRequestObserver
    {
        #region [Data Members]
        private INpp _nppHelper                                       = null;
        private ISettings _settings                                   = null;
        private MouseMonitor _mouseMovementObserver                   = new MouseMonitor();
        private DelayedEventHandler _mouseMovementDelayedEventHandler = null;
        private Tokenizer.TokenTag _previousReeferenceToken           = default(Tokenizer.TokenTag);        
        private bool _isKeyboardShortCutActive                        = false;
        private bool _highLightToken                                  = false;
        private IWin32 _win32Helper                                   = null;
        #endregion

        #region [Events]
        internal event Action MouseMove;

        #endregion

        #region [Interface]
        internal ReferenceRequestObserver(INpp nppHelper, ISettings settings, IWin32 win32helper)
        {
            _nppHelper       = nppHelper;
            _settings        = settings;
            _win32Helper     = win32helper;
            _mouseMovementObserver.MouseMove += OnMouseMovementObserverMouseMove;            
            IsKeyboardShortCutActive = false;
            _mouseMovementDelayedEventHandler = new DelayedEventHandler(new ActionWrapper(MouseMovementStabilized), 250);
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
                        CancelPendingRequest();
                        HideUnderlinedToken();
                    }
                }
            }
        }

        public Tokenizer.TokenTag UnderlinedToken
        {
            get
            {
                return _previousReeferenceToken;
            }
            private set
            {
                _previousReeferenceToken = value;
            }
        }

        //todo ignore pending response from backend
        internal void CancelPendingRequest()
        {
            _mouseMovementDelayedEventHandler.Cancel();
        }
        #endregion

        private void UnderlineToken()
        {
            int aReferenceColor = GetReferenceLinkColor();
            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle,   SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReeferenceToken.Type, 1);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReeferenceToken.Type, 1);

            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);



            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTACTIVEFORE, 1, aReferenceColor);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTACTIVEFORE, 1, aReferenceColor);
            _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_STARTSTYLING, _previousReeferenceToken.BufferPosition, 0);
            _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_SETSTYLING, _previousReeferenceToken.EndColumn - _previousReeferenceToken.StartColumn, (int)_previousReeferenceToken.Type);
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
                _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReeferenceToken.Type, 0);
                _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReeferenceToken.Type, 0);
                _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_STARTSTYLING, _previousReeferenceToken.BufferPosition, 0);
                _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_SETSTYLING, _previousReeferenceToken.EndColumn - _previousReeferenceToken.StartColumn, (int)_previousReeferenceToken.Type);
                _highLightToken = false;
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X - 1, System.Windows.Forms.Cursor.Position.Y);
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X + 1, System.Windows.Forms.Cursor.Position.Y);
            }
        }

        private void MouseMovementStabilized()
        {
            //Tokenizer.TokenTag aTokenUnderCursor = FindTokenUnderCursor();
            //if(!aTokenUnderCursor.Equals(_previousReeferenceToken) && !String.IsNullOrEmpty(aTokenUnderCursor.Context) && _actualToken.Equals(aTokenUnderCursor))
            //{
            //    _previousReeferenceToken = aTokenUnderCursor;
            //    UnderlineToken();
            //    _highLightToken = true;
            //}
            //else if(!aTokenUnderCursor.Equals(_previousReeferenceToken))
            //{
            //    //either null or empty, or cursor points somewhere else
            //    HideUnderlinedToken();
            //}
        }

        private void OnMouseMovementObserverMouseMove()
        {
            if (MouseMove != null)
            {
                MouseMove();
            }
            //if (_isKeyboardShortCutActive && FileUtilities.IsRTextFile(_settings, _nppHelper))
            //{
            //    _mouseMovementDelayedEventHandler.TriggerHandler();
            //    _actualToken = FindTokenUnderCursor();
            //    if(_highLightToken)
            //    {
            //        if(!_actualToken.Equals(_previousReeferenceToken))
            //        {
            //            HideUnderlinedToken();
            //            _highLightToken = false;
            //        }
            //    }
            //}
            //else
            //{
            //    _highLightToken = false;
            //    HideUnderlinedToken();
            //}
        }        
    }
}
