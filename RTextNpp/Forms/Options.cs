using System.Windows.Forms;
using RTextNppPlugin.Utilities;

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
        }

        public void RestoreSettings()
        {
            AutoLoadWorkspace = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoLoadWorkspace);
            AutoSaveFiles = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoSaveFiles);
            AutoChangeWorkspace = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoChangeWorkspace);
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
    }
}
