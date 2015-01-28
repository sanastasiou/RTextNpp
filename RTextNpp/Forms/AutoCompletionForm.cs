using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    }
}
