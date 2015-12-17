using RTextNppPlugin.DllExport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTextNppPlugin.Scintilla.Annotations
{
    /**
     * \brief   A line visibility observer.
     *          This class observes the active document and informs clients about changes in the visible lines.
     *          This can be used to e.g. display error annotations only for visible lines thus, massively improving performance
     *          when thousands of errors are present.
     */
    internal class LineVisibilityObserver : IDisposable, ILineVisibilityObserver
    {
        #region [Data Members]
        private INpp _nppHelper                       = null;
        private bool _disposed                        = false;
        private VisibilityInfo _currentVisibilityInfo = null;
        #endregion

        #region [Events]
        public event VisibilityInfoUpdated OnVisibilityInfoUpdated;
        #endregion

        #region [Interface]
        internal LineVisibilityObserver(INpp nppHelper, Plugin plugin)
        {
            _nppHelper                   = nppHelper;
            plugin.BufferActivated       += OnBufferActivated;
            plugin.ScintillaFocusChanged += OnScintillaFocusChanged;
            plugin.ScintillaUiUpdated    += OnScintillaUiUpdated;
        }

        #region [ILineVisibilityObserver Members]
        public VisibilityInfo VisibilityInfo
        {
            get
            {
                return _currentVisibilityInfo;
            }
            private set
            {
                if (value != _currentVisibilityInfo)
                {
                    _currentVisibilityInfo = value;
                    if (OnVisibilityInfoUpdated != null)
                    {
                        OnVisibilityInfoUpdated(value);
                    }
                }
            }
        }
        #endregion

        #region [IDisposable Members]
        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                Plugin.Instance.BufferActivated       -= OnBufferActivated;
                Plugin.Instance.ScintillaFocusChanged -= OnScintillaFocusChanged;
                Plugin.Instance.ScintillaUiUpdated    -= OnScintillaUiUpdated;
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

        #region [Event Handlers]

        void OnScintillaUiUpdated(SCNotification notification)
        {
            //find activated buffer


            VisibilityInfo = new VisibilityInfo
            {
                File            = _nppHelper.GetActiveFile(notification.nmhdr.hwndFrom),
                ScintillaHandle = notification.nmhdr.hwndFrom,
                FirstLine       = _nppHelper.GetFirstVisibleLine(notification.nmhdr.hwndFrom),
                LastLine        = _nppHelper.GetLastVisibleLine(notification.nmhdr.hwndFrom)
            };
        }

        void OnBufferActivated(object source, string file)
        {
            var sciPtr = _nppHelper.FindScintillaFromFilepath(file);
            VisibilityInfo = new VisibilityInfo
            {
                File            = file,
                ScintillaHandle = sciPtr,
                FirstLine       = _nppHelper.GetFirstVisibleLine(sciPtr),
                LastLine        = _nppHelper.GetLastVisibleLine(sciPtr)
            };
        }

        void OnScintillaFocusChanged(IntPtr sciPtr, bool hasFocus)
        {
            //if(hasFocus)
            //{
            //    _focusedEditor = sciPtr;
            //}
        }

        #endregion

        #region [Helpers]

        #endregion
    }
}
