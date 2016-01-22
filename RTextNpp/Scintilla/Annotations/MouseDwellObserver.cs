using RTextNppPlugin.RText.Parsing;
using System;

namespace RTextNppPlugin.Scintilla.Annotations
{
    /**
     * \brief   A mouse dwell observer.
     *          The purpose of this class is none other, as to notify clients that mouse is dwelling over a certain token of text.
     *          Client can then decide if e.g. tooltips should appear.
     */
    internal class MouseDwellObserver : IDisposable, RTextNppPlugin.Scintilla.Annotations.IMouseDwellObserver
    {
        #region [Data Members]
        private Tokenizer.TokenTag _dwelledToken = default(Tokenizer.TokenTag);
        private INpp _nppHelper                  = null;
        private bool _disposed                   = false;
        private string _activeFile               = string.Empty;
        private View _activeView                 = View.Main;
        #endregion

        #region [Interface]

        #region [Events]
        public delegate void DwellStartingCallback(Tokenizer.TokenTag token, string file, RTextNppPlugin.Scintilla.View View);
        public delegate void DwellEndingCallback();

        public event DwellStartingCallback OnDwellStartingEvent;
        public event DwellEndingCallback OnDwellEndingEvent;
        #endregion

        internal MouseDwellObserver(Plugin plugin, INpp nppHelper)
        {
            plugin.OnDwellStarting += OnDwellStarting;
            plugin.OnDwellEnding   += OnDwellEnding;
            _nppHelper             = nppHelper;
        }

        void OnDwellEnding(IntPtr sciPtr, int position, System.Drawing.Point point)
        {
            if(OnDwellEndingEvent != null)
            {
                OnDwellEndingEvent();
            }
            _dwelledToken = default(Tokenizer.TokenTag);
        }

        void OnDwellStarting(IntPtr sciPtr, int position, System.Drawing.Point point)
        {
            _activeView  = _nppHelper.CurrentView;
            _activeFile  = _nppHelper.GetActiveFile(sciPtr);
            ToolTipToken = Tokenizer.FindTokenUnderCursor(position, _nppHelper, sciPtr);
        }

        internal Tokenizer.TokenTag ToolTipToken
        {
            get
            {
                return _dwelledToken;
            }
            private set
            {
                if(!_dwelledToken.Equals(value) && !value.Equals(default(Tokenizer.TokenTag)))
                {
                    _dwelledToken = value;
                    if (OnDwellStartingEvent != null)
                    {
                        OnDwellStartingEvent(value, _activeFile, _activeView);
                    }
                }
            }
        }
        #region [IDisposable Members]
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                Plugin.Instance.OnDwellStarting -= OnDwellStarting;
                Plugin.Instance.OnDwellEnding   -= OnDwellEnding;
            }
            _disposed = true;
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #endregion
    }
}
