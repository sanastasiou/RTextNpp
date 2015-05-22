using System;
using System.Windows.Forms;
using System.ComponentModel;
using RTextNppPlugin.WpfControls;
using RTextNppPlugin.RText;

namespace RTextNppPlugin
{
    partial class ConsoleOutputForm : Form
    {
        internal ConsoleOutputForm(ConnectorManager cmanager)
        {
            _consoleOutputHost = new ElementHost<WpfControls.ConsoleOutput, ViewModels.ConsoleViewModel>(new ConsoleOutput(cmanager));
            InitializeComponent();            
        }

        private System.Windows.Forms.Integration.ElementHost _consoleOutputHost;
    }
}
