using System;
using System.Diagnostics;


namespace RTextNppPlugin.RText
{
    using CSScriptIntellisense;
    using RText.Parsing;
    using Utilities;
    using Utilities.Settings;
    using DllExport;
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
            _mouseMovementDelayedEventHandler = new DelayedEventHandler(new ActionWrapper(MouseMovementStabilized), 500);
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

        private void UnderlineToken(Tokenizer.TokenTag t)
        {
            //_win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_STYLESETHOTSPOT, 3, 1);
            //_win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_STYLESETHOTSPOT, 3, 1);
            //_win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_STYLESETFORE, 1, 0xFFFFFF);
            //_win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_STYLESETFORE, 1, 0xFFFFFF);
            //_win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            //_win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            //_win32Helper.ISendMessage(Plugin.nppData._scintillaMainHandle, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);
            //_win32Helper.ISendMessage(Plugin.nppData._scintillaSecondHandle, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);
            //_win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_STARTSTYLING, t.BufferPosition, 0);
            //_win32Helper.ISendMessage(_nppHelper.GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_SETSTYLING, t.EndColumn - t.StartColumn, 3);
        }

        private void MouseMovementStabilized()
        {
            Tokenizer.TokenTag aTokenUnderCursor = FindTokenUnderCursor();
            if(!aTokenUnderCursor.Equals(_previousReeferenceToken) && !String.IsNullOrEmpty(aTokenUnderCursor.Context) && _actualToken.Equals(aTokenUnderCursor))
            {
                Trace.WriteLine("Highlighting Token...");
                UnderlineToken(aTokenUnderCursor);
                _highLightToken = true;
                if(OnLinkReferenceRequested != null)
                {
                    OnLinkReferenceRequested(this, new ReferenceRequestEvent {  ReferenceToken = aTokenUnderCursor});
                }
            }
            _previousReeferenceToken = aTokenUnderCursor;
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
                        Trace.WriteLine("Remove highlighting from token...");
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
                    _highLightToken = false;
                    Trace.WriteLine("Remove highlighting from token...");
                }                
            }
        }
    }
}
