using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
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
        private INpp _nppHelper                                         = null;
        private bool _disposed                                          = false;
        private VisibilityInfo _mainVisibilityInfo                      = null;
        private VisibilityInfo _subVisibilityInfo                       = null;
        #endregion

        #region [Events]
        public event VisibilityInfoUpdated OnVisibilityInfoUpdated;
        #endregion

        #region [Interface]
        internal LineVisibilityObserver(INpp nppHelper, Plugin plugin)
        {
            _nppHelper                  = nppHelper;
            plugin.ScintillaUiUpdated   += OnScintillaUiUpdated;
        }

        #region [ILineVisibilityObserver Members]
        public VisibilityInfo MainVisibilityInfo
        {
            get
            {
                return _mainVisibilityInfo;
            }
            private set
            {
                if (value != _mainVisibilityInfo)
                {
                    _mainVisibilityInfo = value;
                    UpdateInfo(value, _nppHelper.MainScintilla);
                }
            }
        }

        public VisibilityInfo SubVisibilityInfo
        {
            get
            {
                return _subVisibilityInfo;
            }
            private set
            {
                if (value != _subVisibilityInfo)
                {
                    _subVisibilityInfo = value;
                    UpdateInfo(value, _nppHelper.SecondaryScintilla);
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
                Plugin.Instance.ScintillaUiUpdated -= OnScintillaUiUpdated;
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
            string aActiveFileMain = _nppHelper.GetActiveFile(_nppHelper.MainScintilla);
            string aAciveFileSub   = _nppHelper.GetActiveFile(_nppHelper.SecondaryScintilla);
            if (AreClonedViewsActive(aActiveFileMain, aAciveFileSub))
            {
                UpdateClonedViewsVisibilityInfo(aActiveFileMain);
            }
            else
            { 
                UpdateVisibilityInfo(new VisibilityInfo
                {
                    File            = _nppHelper.GetActiveFile(notification.nmhdr.hwndFrom),
                    ScintillaHandle = notification.nmhdr.hwndFrom,
                    FirstLine       = _nppHelper.GetFirstVisibleLine(notification.nmhdr.hwndFrom),
                    LastLine        = _nppHelper.GetLastVisibleLine(notification.nmhdr.hwndFrom)
                });
            }
        }

        public void OnBufferActivated(string file, View view)
        {
            if (AreClonedViewsActive(_nppHelper.GetActiveFile(_nppHelper.MainScintilla), _nppHelper.GetActiveFile(_nppHelper.SecondaryScintilla)))
            {
                UpdateClonedViewsVisibilityInfo(file);
            }
            else
            {
                var sciPtr = view == View.Main ? _nppHelper.MainScintilla : _nppHelper.SecondaryScintilla;
                UpdateVisibilityInfo(new VisibilityInfo
                {
                    File            = file,
                    ScintillaHandle = sciPtr,
                    FirstLine       = _nppHelper.GetFirstVisibleLine(sciPtr),
                    LastLine        = _nppHelper.GetLastVisibleLine(sciPtr)
                });
            }
        }

        #endregion

        #region [Helpers]
        void UpdateVisibilityInfo(VisibilityInfo info)
        {
            if(info.ScintillaHandle == _nppHelper.MainScintilla)
            {
                MainVisibilityInfo = info;
            }
            else
            {
                SubVisibilityInfo = info;
            }
        }

        void UpdateInfo(VisibilityInfo info, IntPtr sciPtr)
        {
            if (OnVisibilityInfoUpdated != null)
            {
                OnVisibilityInfoUpdated(info, sciPtr);
            }
        }

        bool AreClonedViewsActive(string mainViewFile, string subViewFile)
        {
            if(AreBothViewsActive())
            {
                return (mainViewFile.Equals(subViewFile, StringComparison.InvariantCultureIgnoreCase));
            }
            return false;
        }

        bool AreBothViewsActive()
        {
            return (_nppHelper.CurrentDocIndex(_nppHelper.MainScintilla) != -1 && _nppHelper.CurrentDocIndex(_nppHelper.SecondaryScintilla) != -1);
        }

        VisibilityInfo CreatedClonedVisibilityInfo(IntPtr sciPtr, string file, int minLine, int maxLine)
        {
            return new VisibilityInfo { File = file, FirstLine = minLine, LastLine = maxLine, ScintillaHandle = sciPtr };
        }

        void UpdateClonedViewsVisibilityInfo(string activeFile)
        {
            int minFirstVisibleLine = Math.Min(_nppHelper.GetFirstVisibleLine(_nppHelper.MainScintilla), _nppHelper.GetFirstVisibleLine(_nppHelper.SecondaryScintilla));
            int maxLastVisibleLine  = Math.Max(_nppHelper.GetLastVisibleLine(_nppHelper.MainScintilla), _nppHelper.GetLastVisibleLine(_nppHelper.SecondaryScintilla));
            UpdateVisibilityInfo(CreatedClonedVisibilityInfo(_nppHelper.MainScintilla, activeFile, minFirstVisibleLine, maxLastVisibleLine));
            UpdateVisibilityInfo(CreatedClonedVisibilityInfo(_nppHelper.SecondaryScintilla, activeFile, minFirstVisibleLine, maxLastVisibleLine));
        }
        #endregion
    }
}
