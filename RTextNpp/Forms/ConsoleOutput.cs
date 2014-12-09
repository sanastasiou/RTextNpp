using System;
using System.Windows.Forms;
using System.ComponentModel;

namespace RTextNppPlugin
{
    partial class ConsoleOutputForm : Form
    {
        public ConsoleOutputForm()
        {
            InitializeComponent();
        }

        public WpfControls.ConsoleOutputElementHost<WpfControls.ConsoleOutput, ViewModels.ConsoleViewModel> WpfControl
        {
            get
            {
                return _consoleOutputHost;
            }
        }    
    }
}
