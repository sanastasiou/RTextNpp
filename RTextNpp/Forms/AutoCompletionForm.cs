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
            _mouseMonitor.Install();
            _mouseMonitor.MouseClicked += OnMouseMonitorMouseClicked;
            FormClosed += OnAutoCompletionFormClosed;
        }

        #region [Event Handlers]
        
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
         * Event handler. Called by AutoCompletionForm for load events.
         *
         * \param   sender  Source of the event.
         * \param   e       Event information.
         */
        private void AutoCompletionForm_Load(object sender, EventArgs e)
        {
            //needed otherwise element host stays empty - wpf / forms bug
            this._autoCompletionControlHost.Child = new AutoCompletionControl();
            var g = this._autoCompletionControlHost.CreateGraphics();
            g.Dispose();
        }

        void AutoCompletionForm_OnClick(object sender, System.EventArgs e)
        {
            bool aIsMouseInsideForm = ClientRectangle.Contains(PointToClient(Control.MousePosition));
            System.Diagnostics.Trace.WriteLine(String.Format("Auto completion form clicked. Mouse inside form {0}", aIsMouseInsideForm));
        }

        void OnMouseMonitorMouseClicked()
        {
            if (!ClientRectangle.Contains(PointToClient(Control.MousePosition)))
            {
                Visible = false;
            }
        }

        void OnAutoCompletionFormClosed(object sender, FormClosedEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("Auto completion control form closed...");   
        }

        #endregion

        /**
         * Gets or sets a value indicating whether to request an automatic completion list or use the cached list.
         *
         * \return  true if a new list is required, false otherwise.
         */
        public bool RequestAutoCompletionList { get; set; }

        #region [Data Members]
        CSScriptIntellisense.MouseMonitor _mouseMonitor = new CSScriptIntellisense.MouseMonitor();
        #endregion
    }
}
