using RTextNppPlugin.WpfControls;
using RTextNppPlugin.ViewModels;

namespace RTextNppPlugin.Forms
{
    partial class AutoCompletionForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._autoCompletionControlHost = new ElementHost<AutoCompletionControl, AutoCompletionViewModel>();// new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // _autoCompletionControlHost
            // 
            this._autoCompletionControlHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._autoCompletionControlHost.Location = new System.Drawing.Point(0, 0);
            this._autoCompletionControlHost.Name = "_autoCompletionControlHost";
            this._autoCompletionControlHost.Size = new System.Drawing.Size(549, 304);
            this._autoCompletionControlHost.TabIndex = 0;
            this._autoCompletionControlHost.Text = "elementHost1";
            this._autoCompletionControlHost.Child = null;
            // 
            // AutoCompletionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(549, 304);
            this.ControlBox = false;
            this.Controls.Add(this._autoCompletionControlHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AutoCompletionForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "AutoCompletionForm";
            this.Load += new System.EventHandler(this.AutoCompletionForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private ElementHost<AutoCompletionControl, AutoCompletionViewModel> _autoCompletionControlHost;

    }
}