using System;
namespace RTextNppPlugin.Utilities.WpfControlHost
{
    using RTextNppPlugin.Scintilla;
    using RTextNppPlugin.Utilities.Settings;
    internal class PersistentWpfControlHost<T> : WpfControlHostBase<T>, IDisposable where T : System.Windows.Forms.Form
    {
        #region [Interface]
        public PersistentWpfControlHost(Settings.RTextNppSettings persistenceKey, T elementHost, ISettings settings, INpp nppHelper ) : base(elementHost, nppHelper)
        {
            _key      = persistenceKey;
            _settings = settings;
        }
        #endregion
        #region [Implementation Details]
        internal override void OnVisibilityChanged(object sender, EventArgs e)
        {
            base.OnVisibilityChanged(sender, e);
            _settings.Set(base.Visible, _key);
        }
        #endregion
        #region [Data members]
        readonly Settings.RTextNppSettings _key;
        readonly ISettings _settings;
        #endregion
    }
}