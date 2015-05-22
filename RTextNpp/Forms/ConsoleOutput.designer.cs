using System.ComponentModel;
using RTextNppPlugin.WpfControls;


namespace RTextNppPlugin
{
    partial class ConsoleOutputForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            SuspendLayout();
            // 
            // _consoleOutputHost
            // 
            _consoleOutputHost.Dock = System.Windows.Forms.DockStyle.Fill;
            _consoleOutputHost.Location = new System.Drawing.Point(0, 0);
            _consoleOutputHost.Name = "_consoleOutputHost";
            _consoleOutputHost.Size = new System.Drawing.Size(680, 186);
            _consoleOutputHost.TabIndex = 0;
            _consoleOutputHost.Text = "ConsoleOutputHost";
            // 
            // ConsoleOutputForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(680, 186);
            Controls.Add(_consoleOutputHost);
            Name = "ConsoleOutputForm";
            Text = "RText++ Console";
            ResumeLayout(false);
        }

        #endregion        
    }
}