namespace RTextNppPlugin.Forms
{
    partial class Options
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
            this.CancelOptionsButton = new System.Windows.Forms.Button();
            this.SaveOptionsButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this._autoloadWorkspaceCheckButton = new System.Windows.Forms.CheckBox();
            this._autoSaveFileCheckBox = new System.Windows.Forms.CheckBox();
            this._autoSelectWorkspaceCheckBox = new System.Windows.Forms.CheckBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.groupBox1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // CancelOptionsButton
            // 
            this.CancelOptionsButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelOptionsButton.Location = new System.Drawing.Point(448, 390);
            this.CancelOptionsButton.Name = "CancelOptionsButton";
            this.CancelOptionsButton.Size = new System.Drawing.Size(75, 23);
            this.CancelOptionsButton.TabIndex = 0;
            this.CancelOptionsButton.Text = "Cancel";
            this.CancelOptionsButton.UseVisualStyleBackColor = true;
            // 
            // SaveOptionsButton
            // 
            this.SaveOptionsButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.SaveOptionsButton.Location = new System.Drawing.Point(357, 390);
            this.SaveOptionsButton.Name = "SaveOptionsButton";
            this.SaveOptionsButton.Size = new System.Drawing.Size(75, 23);
            this.SaveOptionsButton.TabIndex = 2;
            this.SaveOptionsButton.Text = "Save";
            this.SaveOptionsButton.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tabControl1);
            this.groupBox1.Location = new System.Drawing.Point(12, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(520, 377);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "RText++ Options";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(6, 19);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(507, 352);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox3);
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(499, 326);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Error Settings";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.textBox2);
            this.groupBox3.Controls.Add(this.textBox1);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Location = new System.Drawing.Point(105, 6);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(249, 81);
            this.groupBox3.TabIndex = 1;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Error Options";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(146, 45);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(93, 20);
            this.textBox2.TabIndex = 3;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(146, 19);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(93, 20);
            this.textBox1.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(134, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Max number of error lines : ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(133, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Max number of errors       : ";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButton2);
            this.groupBox2.Controls.Add(this.radioButton1);
            this.groupBox2.Location = new System.Drawing.Point(6, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(93, 81);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Error Reporting";
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(7, 43);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(60, 17);
            this.radioButton2.TabIndex = 2;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Disable";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(7, 20);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(58, 17);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Enable";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this._autoloadWorkspaceCheckButton);
            this.tabPage2.Controls.Add(this._autoSaveFileCheckBox);
            this.tabPage2.Controls.Add(this._autoSelectWorkspaceCheckBox);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(499, 326);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Workspace Settings";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // _autoloadWorkspaceCheckButton
            // 
            this._autoloadWorkspaceCheckButton.AutoSize = true;
            this._autoloadWorkspaceCheckButton.Location = new System.Drawing.Point(6, 52);
            this._autoloadWorkspaceCheckButton.Name = "_autoloadWorkspaceCheckButton";
            this._autoloadWorkspaceCheckButton.Size = new System.Drawing.Size(224, 17);
            this._autoloadWorkspaceCheckButton.TabIndex = 2;
            this._autoloadWorkspaceCheckButton.Text = "Automatically load workspace on file open";
            this._autoloadWorkspaceCheckButton.UseVisualStyleBackColor = true;
            this._autoloadWorkspaceCheckButton.CheckedChanged += new System.EventHandler(this.AutoLoadWorkspaceOnCheckedChanged);
            // 
            // _autoSaveFileCheckBox
            // 
            this._autoSaveFileCheckBox.AutoSize = true;
            this._autoSaveFileCheckBox.Location = new System.Drawing.Point(6, 29);
            this._autoSaveFileCheckBox.Name = "_autoSaveFileCheckBox";
            this._autoSaveFileCheckBox.Size = new System.Drawing.Size(251, 17);
            this._autoSaveFileCheckBox.TabIndex = 1;
            this._autoSaveFileCheckBox.Text = "Automatically save all open files of a workspace";
            this._autoSaveFileCheckBox.UseVisualStyleBackColor = true;
            this._autoSaveFileCheckBox.CheckedChanged += new System.EventHandler(this.AutoSaveAllOpenFilesCheckBoxOnCheckedChanged);
            // 
            // _autoSelectWorkspaceCheckBox
            // 
            this._autoSelectWorkspaceCheckBox.AutoSize = true;
            this._autoSelectWorkspaceCheckBox.Location = new System.Drawing.Point(6, 6);
            this._autoSelectWorkspaceCheckBox.Name = "_autoSelectWorkspaceCheckBox";
            this._autoSelectWorkspaceCheckBox.Size = new System.Drawing.Size(206, 17);
            this._autoSelectWorkspaceCheckBox.TabIndex = 0;
            this._autoSelectWorkspaceCheckBox.Text = "Automatically select active workspace";
            this._autoSelectWorkspaceCheckBox.UseVisualStyleBackColor = true;
            this._autoSelectWorkspaceCheckBox.CheckedChanged += new System.EventHandler(this.AutoSelectActiveWorkspaceOnCheckdChanged);
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(499, 326);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "References";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(499, 326);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Auto Completion";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 417);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.SaveOptionsButton);
            this.Controls.Add(this.CancelOptionsButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Options";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Options";
            this.groupBox1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button CancelOptionsButton;
        private System.Windows.Forms.Button SaveOptionsButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox _autoloadWorkspaceCheckButton;
        private System.Windows.Forms.CheckBox _autoSaveFileCheckBox;
        private System.Windows.Forms.CheckBox _autoSelectWorkspaceCheckBox;
    }
}