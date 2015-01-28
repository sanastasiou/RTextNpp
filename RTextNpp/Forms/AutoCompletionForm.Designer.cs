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
            this._autoCompletionListHost = new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // _autoCompletionListHost
            // 
            this._autoCompletionListHost.AutoSize = true;
            this._autoCompletionListHost.Location = new System.Drawing.Point(12, 12);
            this._autoCompletionListHost.Name = "_autoCompletionListHost";
            this._autoCompletionListHost.Size = new System.Drawing.Size(1, 1);
            this._autoCompletionListHost.TabIndex = 0;
            this._autoCompletionListHost.Text = "elementHost1";
            this._autoCompletionListHost.Child = null;
            // 
            // AutoCompletionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.ControlBox = false;
            this.Controls.Add(this._autoCompletionListHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AutoCompletionForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "AutoCompletionForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost _autoCompletionListHost = new ConsoleOutputElementHost<AutoCompletionControl, AutoCompletionViewModel>();
    }
}