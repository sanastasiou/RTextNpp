namespace NppPluginNET
{
    partial class frmGoToLine
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
            this.ConsoleOutput = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ConsoleOutput
            // 
            this.ConsoleOutput.Location = new System.Drawing.Point(13, 13);
            this.ConsoleOutput.Multiline = true;
            this.ConsoleOutput.Name = "ConsoleOutput";
            this.ConsoleOutput.ReadOnly = true;
            this.ConsoleOutput.Size = new System.Drawing.Size(655, 161);
            this.ConsoleOutput.TabIndex = 0;
            this.ConsoleOutput.Text = "Initial Text";
            // 
            // frmGoToLine
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(680, 186);
            this.Controls.Add(this.ConsoleOutput);
            this.Name = "frmGoToLine";
            this.Text = "JEP Output";
            this.VisibleChanged += new System.EventHandler(this.FrmGoToLineVisibleChanged);
            this.ResumeLayout(false);
            this.PerformLayout();
            this.ConsoleOutput.Text = "Some another Initial Text";
        }


        public System.Windows.Forms.TextBox Console
        {
            get
            {
                return ConsoleOutput;
            }
        }
        
        #endregion

        private System.Windows.Forms.TextBox ConsoleOutput;





    }
}