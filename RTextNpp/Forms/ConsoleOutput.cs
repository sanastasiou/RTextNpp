using RTextNppPlugin.RText;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.WpfControls;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using RTextNppPlugin.Utilities.Settings;

namespace RTextNppPlugin.Forms
{
    [ExcludeFromCodeCoverage]
    partial class ConsoleOutputForm : Form
    {
        internal ConsoleOutputForm(ConnectorManager cmanager, INpp nppHelper, IStyleConfigurationObserver styleObserver)
        {
            _consoleOutputHost = new ElementHost<WpfControls.ConsoleOutput, ViewModels.ConsoleViewModel>(new ConsoleOutput(cmanager, nppHelper, styleObserver));
            InitializeComponent();            
        }

        private System.Windows.Forms.Integration.ElementHost _consoleOutputHost;
    }
}
