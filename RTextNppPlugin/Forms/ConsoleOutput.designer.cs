namespace RTextNppPlugin
{
    partial class ConsoleOutputForm
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
            this._consoleOutputHost = new WpfControls.ConsoleOutputElementHost<WpfControls.ConsoleOutput, ViewModels.ConsoleViewModel>();
            this.SuspendLayout();
            // 
            // _consoleOutputHost
            // 
            this._consoleOutputHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._consoleOutputHost.Location = new System.Drawing.Point(0, 0);
            this._consoleOutputHost.Name = "_consoleOutputHost";
            this._consoleOutputHost.Size = new System.Drawing.Size(680, 186);
            this._consoleOutputHost.TabIndex = 0;
            this._consoleOutputHost.Text = "ConsoleOutputHost";
            // 
            // ConsoleOutputForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(680, 186);
            this.Controls.Add(this._consoleOutputHost);
            this.Name = "ConsoleOutputForm";
            this.Text = "RText++ Console";
            this.ResumeLayout(false);

        }

        #endregion

        private WpfControls.ConsoleOutputElementHost<WpfControls.ConsoleOutput, ViewModels.ConsoleViewModel> _consoleOutputHost;






    }
}