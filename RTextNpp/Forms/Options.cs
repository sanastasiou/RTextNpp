using System.Windows.Forms;
using RTextNppPlugin.Utilities;
using System.Text.RegularExpressions;
using RTextNppPlugin.Utilities.Settings;

namespace RTextNppPlugin.Forms
{
    internal partial class Options : Form
    {
        #region [Data Members]
        ISettings _settings = null;
        #endregion

        internal Options(ISettings settings)
        {
            _settings = settings;
            InitializeComponent();
        }

        internal void SaveSettings()
        {
            _settings.Set(AutoChangeWorkspace, Settings.RTextNppSettings.AutoChangeWorkspace);
            _settings.Set(AutoLoadWorkspace, Settings.RTextNppSettings.AutoLoadWorkspace);
            _settings.Set(AutoSaveFiles, Settings.RTextNppSettings.AutoSaveFiles);
            _settings.Set(_excludeExtensionsTextBox.Text, Settings.RTextNppSettings.ExcludeExtensions);
        }

        internal void RestoreSettings()
        {
            AutoLoadWorkspace              = _settings.Get<bool>(Settings.RTextNppSettings.AutoLoadWorkspace);
            AutoSaveFiles                  = _settings.Get<bool>(Settings.RTextNppSettings.AutoSaveFiles);
            AutoChangeWorkspace            = _settings.Get<bool>(Settings.RTextNppSettings.AutoChangeWorkspace);
            _excludeExtensionsTextBox.Text = _settings.Get(Settings.RTextNppSettings.ExcludeExtensions);
        }

        internal bool AutoLoadWorkspace
        {
            get
            {
                return _autoloadWorkspaceCheckButton.Checked;
            }
            private set
            {
                _autoloadWorkspaceCheckButton.Checked = value;
            }
        }

        internal bool AutoSaveFiles
        {
            get
            {
                return _autoSaveFileCheckBox.Checked;
            }
            private set
            {
                _autoSaveFileCheckBox.Checked = value;
            }
        }

        internal bool AutoChangeWorkspace
        {
            get
            {
                return _autoSelectWorkspaceCheckBox.Checked;
            }
            private set
            {
                _autoSelectWorkspaceCheckBox.Checked = value;
            }
        }

        private void OnOptionsFormLoad(object sender, System.EventArgs e)
        {
            RestoreSettings();
            _tooltipPlaceholder.SetToolTip(_autoloadWorkspaceCheckButton, "Check to automatically load the corresponding workspace of an automate file upon opening the file.");
            _tooltipPlaceholder.SetToolTip(_autoSaveFileCheckBox, "Check to automatically save all relevant files of a workspace if any workspace file is modified.");
            _tooltipPlaceholder.SetToolTip(_autoSelectWorkspaceCheckBox, "Check to automatically select the correct workspace base on the currently viewed file.");
            _tooltipPlaceholder.SetToolTip(_excludeExtensionsTextBox, "Added extensions to be excluded without a dot, separated by ; e.g. meta; .");
            AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;            
        }

        private void OnValidatingExcludedExtensions(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Regex.IsMatch(_excludeExtensionsTextBox.Text, @"^(\s*|(\w+;)*\w+;?)$"))
            {
                _exludeExtensionsErrorProvider.SetError(_excludeExtensionsTextBox, "Type in extensions without (.) followed by (;)");
                e.Cancel = true;
            }
            else
            {
                _exludeExtensionsErrorProvider.SetError(_excludeExtensionsTextBox, "");
                e.Cancel = false;
            }
        }
    }
}
