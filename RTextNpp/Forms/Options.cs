using System.Windows.Forms;
using RTextNppPlugin.Utilities;
using System.Text.RegularExpressions;

namespace RTextNppPlugin.Forms
{
    public partial class Options : Form
    {
        public Options()
        {
            InitializeComponent();
            RestoreSettings();
        }

        public void SaveSettings()
        {
            Settings.Instance.Set(AutoChangeWorkspace, Settings.RTextNppSettings.AutoChangeWorkspace);
            Settings.Instance.Set(AutoLoadWorkspace, Settings.RTextNppSettings.AutoLoadWorkspace);
            Settings.Instance.Set(AutoSaveFiles, Settings.RTextNppSettings.AutoSaveFiles);
            Settings.Instance.Set(_excludeExtensionsTextBox.Text, Settings.RTextNppSettings.ExcludeExtensions);
        }

        public void RestoreSettings()
        {
            AutoLoadWorkspace = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoLoadWorkspace);
            AutoSaveFiles = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoSaveFiles);
            AutoChangeWorkspace = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoChangeWorkspace);
            _excludeExtensionsTextBox.Text = Settings.Instance.Get(Settings.RTextNppSettings.ExcludeExtensions);
        }

        public bool AutoLoadWorkspace
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

        public bool AutoSaveFiles
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

        public bool AutoChangeWorkspace
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
