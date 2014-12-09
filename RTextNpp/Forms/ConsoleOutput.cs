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
            this.Move += ConsoleOutputForm_Move;
            this.Resize += ConsoleOutputForm_Move;
        }

        void ConsoleOutputForm_Move(object sender, EventArgs e)
        {
            if(_consoleOutputHost != null)
            {
                _consoleOutputHost.Refresh();
            }
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
