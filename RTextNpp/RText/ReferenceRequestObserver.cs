using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RTextNppPlugin.RText
{
    using CSScriptIntellisense;
    using DllExport;
    using Parsing;
    using RTextNppPlugin.Scintilla;
    using System;
    using Utilities;
    using Utilities.Settings;
    using WpfControls;
    class ReferenceRequestObserver
    {
        #region [Data Members]
        private readonly INpp _nppHelper                             = null;                        //!< Interface to Npp message system.
        private readonly ISettings _settings                         = null;                        //!< Interface to RTextNpp settings.
        private readonly MouseMonitor _mouseMovementObserver         = new MouseMonitor();          //!< Low level mouse monitor hook.
        private Tokenizer.TokenTag _previousReferenceToken           = default(Tokenizer.TokenTag); //!< Holds previous highlighted reference token.
        private bool _isKeyboardShortCutActive                       = false;                       //!< Indicates if reference show shortcut key is active.
        private bool _highLightToken                                 = false;                       //!< Whether a reference token is highlighted.
        private readonly IWin32 _win32Helper                         = null;                        //!< Handle to win32 helper instance.
        private readonly ILinkTargetsWindow _refWindow               = null;                        //!< Handle to reference window.
        private readonly VoidDelayedEventHandler _mouseMoveDebouncer = null;                        //!< Debounces mouse movement for a short period of time so that CPU is not taxed.
        private System.Drawing.Point _previousMousePosition          = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
        private IntPtr _editorWithActiveHotspot                      = IntPtr.Zero;                 //!< Holds editor handle, where hotspot is currently active.
        #endregion
        
        #region [Events]
        #endregion
        
        #region [Interface]
        internal ReferenceRequestObserver(INpp nppHelper, ISettings settings, IWin32 win32helper, ILinkTargetsWindow refWindow)
        {
            if(nppHelper == null)
            {
                throw new ArgumentNullException("nppHelper");
            }
            if(settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            if(win32helper == null)
            {
                throw new ArgumentNullException("win32helper");
            }
            if(refWindow == null)
            {
                throw new ArgumentNullException("refWindow");
            }

            _nppHelper                       = nppHelper;
            _settings                        = settings;
            _win32Helper                     = win32helper;
            _mouseMovementObserver.MouseMove += OnMouseMovementObserverMouseMove;
            IsKeyboardShortCutActive         = false;
            _refWindow                       = refWindow;
            _mouseMoveDebouncer              = new VoidDelayedEventHandler(new Action(DoMouseMovementObserverMouseMove), 100);

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
            _editorWithActiveHotspot = _nppHelper.CurrentScintilla;
            _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 1);
            _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_SETHOTSPOTACTIVEUNDERLINE, 1, 0);
            _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_SETHOTSPOTSINGLELINE, 1, 0);
            _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_SETHOTSPOTACTIVEFORE, 1, _nppHelper.GetStyleForeground(_editorWithActiveHotspot, (int)Constants.StyleId.REFERENCE_LINK));
            _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_SETHOTSPOTACTIVEBACK, 1, _nppHelper.GetStyleBackground(_editorWithActiveHotspot, (int)Constants.StyleId.REFERENCE_LINK));
            _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_STARTSTYLING, _previousReferenceToken.BufferPosition, 0);
            _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_SETSTYLING, _previousReferenceToken.EndColumn - _previousReferenceToken.StartColumn, (int)_previousReferenceToken.Type);
            //fix bug which prevents hotspot to be activated when the mouse hasn't been moved
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X + 1, System.Windows.Forms.Cursor.Position.Y);
            _highLightToken = true;
        }
        internal void HideUnderlinedToken()
        {
            if (_highLightToken)
            {
                _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 0);
                _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_STYLESETHOTSPOT, (int)_previousReferenceToken.Type, 0);
                _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_STARTSTYLING, _previousReferenceToken.BufferPosition, 0);
                _win32Helper.ISendMessage(_editorWithActiveHotspot, SciMsg.SCI_SETSTYLING, _previousReferenceToken.EndColumn - _previousReferenceToken.StartColumn, (int)_previousReferenceToken.Type);
                //fix bug which prevents hotspot to be activated when the mouse hasn't been moved
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X - 1, System.Windows.Forms.Cursor.Position.Y);
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

        private void OnMouseMovementObserverMouseMove()
        {
            System.Drawing.Point aCurrentMousePosition = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
            if (_previousMousePosition != aCurrentMousePosition)
            {
                _previousMousePosition = aCurrentMousePosition;
                _mouseMoveDebouncer.TriggerHandler();
            }
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
                        if (!_refWindow.IsVisible && !_highLightToken)
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