using System;


namespace RTextNppPlugin.RText
{
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
        private Tokenizer.TokenTag _actualToken                       = default(Tokenizer.TokenTag);
        private bool _isAltCtrlPressed                                = false;
        private bool _highLightToken                                  = false;
        private IWin32 _win32Helper                                   = null;
        #endregion

        #region [Events]
        internal class ReferenceRequestEvent
        {
            internal Tokenizer.TokenTag ReferenceToken { get; set; }
        }

        internal delegate void LinkReferenceRequested(ReferenceRequestObserver source, ReferenceRequestEvent e);

        internal event LinkReferenceRequested OnLinkReferenceRequested;

        internal delegate void DismissReferenceLinks(ReferenceRequestObserver source);

        internal event DismissReferenceLinks OnDismissReferenceLinks;
        #endregion

        #region [Interface]
        internal ReferenceRequestObserver(INpp nppHelper, ISettings settings, IWin32 win32helper)
        {
            _nppHelper       = nppHelper;
            _settings        = settings;
            _win32Helper     = win32helper;
            _mouseMovementObserver.MouseMove += OnMouseMovementObserverMouseMove;
            _mouseMovementObserver.Install();
            IsAltCtrlPressed = false;
            _mouseMovementDelayedEventHandler = new DelayedEventHandler(new ActionWrapper(MouseMovementStabilized), 250);
        }

        internal bool IsAltCtrlPressed 
        { 
            set
            {
                if(value != _isAltCtrlPressed)
                {
                    _isAltCtrlPressed = value;
                    if(!_isAltCtrlPressed)
                    {
                        CancelPendingRequest();
                        HideUnderlinedToken();
                    }
                }
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
            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle,   SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReeferenceToken.Type, 1);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReeferenceToken.Type, 1);

            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);



            _win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTACTIVEFORE, 1, 0xFFFFFF);
            _win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTACTIVEFORE, 1, 0xFFFFFF);
            _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_STARTSTYLING, _previousReeferenceToken.BufferPosition, 0);
            _win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_SETSTYLING, _previousReeferenceToken.EndColumn - _previousReeferenceToken.StartColumn, 3);
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
            Tokenizer.TokenTag aTokenUnderCursor = FindTokenUnderCursor();
            if(!aTokenUnderCursor.Equals(_previousReeferenceToken) && !String.IsNullOrEmpty(aTokenUnderCursor.Context) && _actualToken.Equals(aTokenUnderCursor))
            {
                _previousReeferenceToken = aTokenUnderCursor;
                UnderlineToken();
                _highLightToken = true;
                if(OnLinkReferenceRequested != null)
                {
                    OnLinkReferenceRequested(this, new ReferenceRequestEvent {  ReferenceToken = aTokenUnderCursor});
                }
            }
            else if(!aTokenUnderCursor.Equals(_previousReeferenceToken))
            {
                //either null or empty, or cursor points somewhere else
                HideUnderlinedToken();
            }
        }

        private Tokenizer.TokenTag FindTokenUnderCursor()
        {
            int aBufferPosition = _nppHelper.GetPositionFromMouseLocation();
            if (aBufferPosition != -1)
            {
                int aCurrentLine = _nppHelper.GetLineNumber(aBufferPosition);
                Tokenizer aTokenizer = new Tokenizer(aCurrentLine, _nppHelper);
                foreach (var t in aTokenizer.Tokenize())
                {
                    if (t.BufferPosition <= aBufferPosition && t.EndPosition >= aBufferPosition)
                    {
                        return t;
                    }
                }
            }
            return default(Tokenizer.TokenTag);
        }

        private void OnMouseMovementObserverMouseMove()
        {
            if (_isAltCtrlPressed && FileUtilities.IsRTextFile(_settings, _nppHelper))
            {
                _mouseMovementDelayedEventHandler.TriggerHandler();
                _actualToken = FindTokenUnderCursor();
                if(_highLightToken)
                {
                    if(!_actualToken.Equals(_previousReeferenceToken))
                    {
                        HideUnderlinedToken();
                        _highLightToken = false;
                        if(OnDismissReferenceLinks != null)
                        {
                            OnDismissReferenceLinks(this);
                        }
                    }
                }
            }
            else
            {
                if (OnDismissReferenceLinks != null && _highLightToken)
                {
                    OnDismissReferenceLinks(this);
                }
                _highLightToken = false;
                HideUnderlinedToken();
            }
        }
    }
}
