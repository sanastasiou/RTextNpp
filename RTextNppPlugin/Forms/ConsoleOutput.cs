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

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar)
                && (e.KeyChar != '\b')
                && (e.KeyChar != '\t')) 
                e.Handled = true;
        }
        
        void FrmGoToLineVisibleChanged(object sender, EventArgs e)
        {
            //if (!Visible)
            //{
            //    Win32.SendMessage(Plugin.NppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK,
            //                      Plugin.FuncItems.Items[Plugin..idFrmGotToLine]._cmdID, 0);
            //}
        }
    }
}
