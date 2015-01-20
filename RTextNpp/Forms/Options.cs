using System.Windows.Forms;
using RTextNppPlugin.Utilities;

namespace RTextNppPlugin.Forms
{
    public partial class Options : Form
    {
        public Options()
        {
            InitializeComponent();
            _autoloadWorkspaceCheckButton.Checked = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoLoadWorkspace);
            _autoSaveFileCheckBox.Checked         = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoSaveFiles);
            _autoSelectWorkspaceCheckBox.Checked  = Settings.Instance.Get<bool>(Settings.RTextNppSettings.AutoChangeWorkspace);

        }

        private void AutoLoadWorkspaceOnCheckedChanged(object sender, System.EventArgs e)
        {
            
        }

        private void AutoSaveAllOpenFilesCheckBoxOnCheckedChanged(object sender, System.EventArgs e)
        {

        }

        private void AutoSelectActiveWorkspaceOnCheckdChanged(object sender, System.EventArgs e)
        {

        }
    }
}
