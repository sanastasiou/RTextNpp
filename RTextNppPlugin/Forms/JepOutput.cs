using System;
using System.Windows.Forms;

namespace NppPluginNET
{
    partial class frmGoToLine : Form
    {
        public frmGoToLine()
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

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
