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
            components = new System.ComponentModel.Container();
            CancelOptionsButton = new System.Windows.Forms.Button();
            SaveOptionsButton = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            tabControl1 = new System.Windows.Forms.TabControl();
            tabPage1 = new System.Windows.Forms.TabPage();
            groupBox3 = new System.Windows.Forms.GroupBox();
            textBox2 = new System.Windows.Forms.TextBox();
            textBox1 = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            groupBox2 = new System.Windows.Forms.GroupBox();
            radioButton2 = new System.Windows.Forms.RadioButton();
            radioButton1 = new System.Windows.Forms.RadioButton();
            tabPage2 = new System.Windows.Forms.TabPage();
            _autoloadWorkspaceCheckButton = new System.Windows.Forms.CheckBox();
            _autoSaveFileCheckBox = new System.Windows.Forms.CheckBox();
            _autoSelectWorkspaceCheckBox = new System.Windows.Forms.CheckBox();
            tabPage3 = new System.Windows.Forms.TabPage();
            tabPage4 = new System.Windows.Forms.TabPage();
            _tooltipPlaceholder = new System.Windows.Forms.ToolTip(components);
            groupBox4 = new System.Windows.Forms.GroupBox();
            groupBox5 = new System.Windows.Forms.GroupBox();
            _excludeExtensionsTextBox = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            _exludeExtensionsErrorProvider = new System.Windows.Forms.ErrorProvider(components);
            groupBox1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            tabPage2.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(_exludeExtensionsErrorProvider)).BeginInit();
            SuspendLayout();
            // 
            // CancelOptionsButton
            // 
            CancelOptionsButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            CancelOptionsButton.Location = new System.Drawing.Point(448, 390);
            CancelOptionsButton.Name = "CancelOptionsButton";
            CancelOptionsButton.Size = new System.Drawing.Size(75, 23);
            CancelOptionsButton.TabIndex = 0;
            CancelOptionsButton.Text = "Cancel";
            CancelOptionsButton.UseVisualStyleBackColor = true;
            // 
            // SaveOptionsButton
            // 
            SaveOptionsButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            SaveOptionsButton.Location = new System.Drawing.Point(357, 390);
            SaveOptionsButton.Name = "SaveOptionsButton";
            SaveOptionsButton.Size = new System.Drawing.Size(75, 23);
            SaveOptionsButton.TabIndex = 2;
            SaveOptionsButton.Text = "Save";
            SaveOptionsButton.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(tabControl1);
            groupBox1.Location = new System.Drawing.Point(12, 8);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(520, 377);
            groupBox1.TabIndex = 4;
            groupBox1.TabStop = false;
            groupBox1.Text = "RText++ Options";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Location = new System.Drawing.Point(6, 19);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(507, 352);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(groupBox3);
            tabPage1.Controls.Add(groupBox2);
            tabPage1.Location = new System.Drawing.Point(4, 22);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new System.Windows.Forms.Padding(3);
            tabPage1.Size = new System.Drawing.Size(499, 326);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Error Settings";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(textBox2);
            groupBox3.Controls.Add(textBox1);
            groupBox3.Controls.Add(label2);
            groupBox3.Controls.Add(label1);
            groupBox3.Location = new System.Drawing.Point(105, 6);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(249, 81);
            groupBox3.TabIndex = 1;
            groupBox3.TabStop = false;
            groupBox3.Text = "Error Options";
            // 
            // textBox2
            // 
            textBox2.Location = new System.Drawing.Point(146, 45);
            textBox2.Name = "textBox2";
            textBox2.Size = new System.Drawing.Size(93, 20);
            textBox2.TabIndex = 3;
            // 
            // textBox1
            // 
            textBox1.Location = new System.Drawing.Point(146, 19);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(93, 20);
            textBox1.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(7, 48);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(134, 13);
            label2.TabIndex = 1;
            label2.Text = "Max number of error lines : ";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(7, 22);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(133, 13);
            label1.TabIndex = 0;
            label1.Text = "Max number of errors       : ";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(radioButton2);
            groupBox2.Controls.Add(radioButton1);
            groupBox2.Location = new System.Drawing.Point(6, 6);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(93, 81);
            groupBox2.TabIndex = 0;
            groupBox2.TabStop = false;
            groupBox2.Text = "Error Reporting";
            // 
            // radioButton2
            // 
            radioButton2.AutoSize = true;
            radioButton2.Location = new System.Drawing.Point(7, 43);
            radioButton2.Name = "radioButton2";
            radioButton2.Size = new System.Drawing.Size(60, 17);
            radioButton2.TabIndex = 2;
            radioButton2.TabStop = true;
            radioButton2.Text = "Disable";
            radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            radioButton1.AutoSize = true;
            radioButton1.Location = new System.Drawing.Point(7, 20);
            radioButton1.Name = "radioButton1";
            radioButton1.Size = new System.Drawing.Size(58, 17);
            radioButton1.TabIndex = 0;
            radioButton1.TabStop = true;
            radioButton1.Text = "Enable";
            radioButton1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(groupBox5);
            tabPage2.Controls.Add(groupBox4);
            tabPage2.Location = new System.Drawing.Point(4, 22);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(3);
            tabPage2.Size = new System.Drawing.Size(499, 326);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Workspace Settings";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // _autoloadWorkspaceCheckButton
            // 
            _autoloadWorkspaceCheckButton.AutoSize = true;
            _autoloadWorkspaceCheckButton.Location = new System.Drawing.Point(6, 65);
            _autoloadWorkspaceCheckButton.Name = "_autoloadWorkspaceCheckButton";
            _autoloadWorkspaceCheckButton.Size = new System.Drawing.Size(224, 17);
            _autoloadWorkspaceCheckButton.TabIndex = 2;
            _autoloadWorkspaceCheckButton.Text = "Automatically load workspace on file open";
            _autoloadWorkspaceCheckButton.UseVisualStyleBackColor = true;
            // 
            // _autoSaveFileCheckBox
            // 
            _autoSaveFileCheckBox.AutoSize = true;
            _autoSaveFileCheckBox.Location = new System.Drawing.Point(6, 42);
            _autoSaveFileCheckBox.Name = "_autoSaveFileCheckBox";
            _autoSaveFileCheckBox.Size = new System.Drawing.Size(251, 17);
            _autoSaveFileCheckBox.TabIndex = 1;
            _autoSaveFileCheckBox.Text = "Automatically save all open files of a workspace";
            _autoSaveFileCheckBox.UseVisualStyleBackColor = true;
            // 
            // _autoSelectWorkspaceCheckBox
            // 
            _autoSelectWorkspaceCheckBox.AutoSize = true;
            _autoSelectWorkspaceCheckBox.Location = new System.Drawing.Point(6, 19);
            _autoSelectWorkspaceCheckBox.Name = "_autoSelectWorkspaceCheckBox";
            _autoSelectWorkspaceCheckBox.Size = new System.Drawing.Size(206, 17);
            _autoSelectWorkspaceCheckBox.TabIndex = 0;
            _autoSelectWorkspaceCheckBox.Text = "Automatically select active workspace";
            _autoSelectWorkspaceCheckBox.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            tabPage3.Location = new System.Drawing.Point(4, 22);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new System.Windows.Forms.Padding(3);
            tabPage3.Size = new System.Drawing.Size(499, 326);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "References";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            tabPage4.Location = new System.Drawing.Point(4, 22);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new System.Windows.Forms.Padding(3);
            tabPage4.Size = new System.Drawing.Size(499, 326);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Auto Completion";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(_autoSelectWorkspaceCheckBox);
            groupBox4.Controls.Add(_autoloadWorkspaceCheckButton);
            groupBox4.Controls.Add(_autoSaveFileCheckBox);
            groupBox4.Location = new System.Drawing.Point(6, 6);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new System.Drawing.Size(278, 91);
            groupBox4.TabIndex = 3;
            groupBox4.TabStop = false;
            groupBox4.Text = "General Settings";
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(label3);
            groupBox5.Controls.Add(_excludeExtensionsTextBox);
            groupBox5.Location = new System.Drawing.Point(6, 103);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new System.Drawing.Size(278, 53);
            groupBox5.TabIndex = 4;
            groupBox5.TabStop = false;
            groupBox5.Text = "Exclude extensions";
            // 
            // _excludeExtensionsTextBox
            // 
            _excludeExtensionsTextBox.Location = new System.Drawing.Point(59, 19);
            _excludeExtensionsTextBox.Name = "_excludeExtensionsTextBox";
            _excludeExtensionsTextBox.Size = new System.Drawing.Size(192, 20);
            _excludeExtensionsTextBox.TabIndex = 5;
            _excludeExtensionsTextBox.Text = "meta;";
            _excludeExtensionsTextBox.Validating += new System.ComponentModel.CancelEventHandler(OnValidatingExcludedExtensions);
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(5, 22);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(51, 13);
            label3.TabIndex = 5;
            label3.Text = "Exclude :";
            // 
            // _exludeExtensionsErrorProvider
            // 
            _exludeExtensionsErrorProvider.ContainerControl = this;
            // 
            // Options
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(540, 417);
            Controls.Add(groupBox1);
            Controls.Add(SaveOptionsButton);
            Controls.Add(CancelOptionsButton);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Options";
            ShowIcon = false;
            ShowInTaskbar = false;
            Text = "Options";
            Load += new System.EventHandler(OnOptionsFormLoad);
            groupBox1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            tabPage2.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(_exludeExtensionsErrorProvider)).EndInit();
            ResumeLayout(false);

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
        private System.Windows.Forms.ToolTip _tooltipPlaceholder;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox _excludeExtensionsTextBox;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ErrorProvider _exludeExtensionsErrorProvider;
    }
}