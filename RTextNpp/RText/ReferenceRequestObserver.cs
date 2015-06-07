using System;
using System.Diagnostics;


namespace RTextNppPlugin.RText
{
    using CSScriptIntellisense;
    using RTextNppPlugin.RText.Parsing;
    using RTextNppPlugin.Utilities;
    using RTextNppPlugin.Utilities.Settings;
    class ReferenceRequestObserver
    {
        #region [Data Members]
        private INpp _nppHelper = null;
        private ISettings _settings = null;
        private MouseMonitor _mouseMovementObserver = new MouseMonitor();
        private DelayedEventHandler _mouseMovementDelayedEventHandler = null;
        private Tokenizer.TokenTag _previousReeferenceToken = default(Tokenizer.TokenTag);
        private bool _isAltCtrlPressed = false;
        #endregion

        #region [Interface]
        internal ReferenceRequestObserver(INpp nppHelper, ISettings settings)
        {
            _nppHelper = nppHelper;
            _settings  = settings;
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



        private void MouseMovementStabilized()
        {
            Tokenizer.TokenTag aTokenUnderCursor = FindTokenUnderCursor();
            if(!aTokenUnderCursor.Equals(_previousReeferenceToken) && !String.IsNullOrEmpty(aTokenUnderCursor.Context))
            {
                Trace.WriteLine(aTokenUnderCursor);
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
            }
        }
    }
}
