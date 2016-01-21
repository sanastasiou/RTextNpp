using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTextNppPlugin.RText.Parsing;
using System.Diagnostics;

namespace RTextNppPlugin.Scintilla.Annotations
{
    /**
     * \brief   A mouse dwell observer.
     *          The purpose of this class is none other, as to notify clients that mouse is dwelling over a certain token of text.
     *          Client can then decide if e.g. tooltips should appear.
     */
    internal class MouseDwellObserver : IDisposable
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
        internal delegate void DwellStartingCallback(Tokenizer.TokenTag token, string file, RTextNppPlugin.Scintilla.View View);
        internal delegate void DwellEndingCallback();

        internal event DwellStartingCallback OnDwellStartingEvent;
        internal event DwellEndingCallback OnDwellEndingEvent;
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
                if(!_dwelledToken.Equals(value) && !_dwelledToken.Equals(default(Tokenizer.TokenTag)))
                {
                    _dwelledToken = value;
                    if (OnDwellStartingEvent != null)
                    {
                        Trace.WriteLine(String.Format("Dwelling on : {0}", value));
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
