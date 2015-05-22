using System;

namespace RTextNppPlugin.Utilities.WpfControlHost
{
    class PersistentWpfControlHost<T> : WpfControlHostBase<T>, IDisposable where T : System.Windows.Forms.Form
    {
        #region [Interface]

        public PersistentWpfControlHost(Settings.RTextNppSettings persistenceKey, T elementHost, Settings settings ) : base(elementHost)
        {
            _key      = persistenceKey;
            _settings = settings;
        }

        #endregion

        #region [Implementation Details]
        protected override void OnVisibilityChanged(object sender, EventArgs e)
        {
            base.OnVisibilityChanged(sender, e);
            _settings.Set(base.Visible, _key);
        }

        #endregion

        #region [Data members]
        readonly Settings.RTextNppSettings _key;
        readonly Settings _settings;
        #endregion
    }
}
