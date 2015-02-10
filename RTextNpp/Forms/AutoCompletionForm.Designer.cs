using RTextNppPlugin.WpfControls;
using RTextNppPlugin.ViewModels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System;

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
            //this._autoCompletionControlHost = new ElementHost<AutoCompletionControl, AutoCompletionViewModel>();// new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // _autoCompletionControlHost
            // 
            this._autoCompletionControlHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._autoCompletionControlHost.Location = new System.Drawing.Point(0, 0);
            this._autoCompletionControlHost.Name = "_autoCompletionControlHost";
            this._autoCompletionControlHost.Size = new System.Drawing.Size(0, 0);
            this._autoCompletionControlHost.AutoSize = true;
            this._autoCompletionControlHost.TabIndex = 0;
            this._autoCompletionControlHost.Text = "elementHost1";
            this._autoCompletionControlHost.ViewModel.Host = this;
            // 
            // AutoCompletionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(0, 0);
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
            this.Click += AutoCompletionForm_OnClick;
            this.ResumeLayout(false);
        }

        #endregion

        #region [Interface]
        internal AutoCompletionViewModel AutoCompletionViewModel
        {
            get
            {
                return _autoCompletionControlHost.ViewModel;
            }
        }

        internal AutoCompletionControl AutoCompletionWpfControl
        {
            get
            {
                return _autoCompletionControlHost.WpfControl;
            }
        }

        public void ResizeToWpfSize()
        {
            var wpfSize     = GetElementPixelSize(_autoCompletionControlHost.Child);
            int pixelWidth  = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, wpfSize.Width));
            int pixelHeight = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, wpfSize.Height));
            this.ClientSize = new System.Drawing.Size(pixelWidth, pixelHeight);
        }

        #endregion


        #region [Helprs]
        private System.Windows.Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
            {
                transformToDevice = source.CompositionTarget.TransformToDevice;
            }
            else
            {
                using (var Hwndsource = new HwndSource(new HwndSourceParameters()))
                {
                    transformToDevice = Hwndsource.CompositionTarget.TransformToDevice;
                }
            }

            if (element.DesiredSize == new System.Windows.Size())
            {
                element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            }
            return (System.Windows.Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }
        #endregion

        #region [Data Members]
        private ElementHost<AutoCompletionControl, AutoCompletionViewModel> _autoCompletionControlHost = new ElementHost<AutoCompletionControl, AutoCompletionViewModel>();
        #endregion        
    }
}