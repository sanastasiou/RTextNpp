using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RTextNppPlugin.WpfControls;
using RTextNppPlugin.ViewModels;

namespace RTextNppPlugin.Forms
{
    public partial class AutoCompletionForm : Form
    {
        public AutoCompletionForm()
        {
            InitializeComponent();
        }

        /**
         * Gets a value indicating whether the without activation is shown.
         *
         * \return  True if the window will not be activated when it is shown; otherwise, false. The
         *          default is false.
         *
         *  Gets a value indicating whether the window will be activated when it is shown.
         */
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }

        /**
         * Gets or sets a value indicating whether to request an automatic completion list or use the cached list.
         *
         * \return  true if a new list is required, false otherwise.
         */
        public bool RequestAutoCompletionList { get; set; }

        private void AutoCompletionForm_Load(object sender, EventArgs e)
        {
            this._autoCompletionControlHost.Child = new AutoCompletionControl();
            var g = this._autoCompletionControlHost.CreateGraphics();
            g.Dispose();
        }
    }
}
