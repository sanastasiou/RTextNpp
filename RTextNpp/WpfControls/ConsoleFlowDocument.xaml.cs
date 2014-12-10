/**
 * \file    WpfControls\ConsoleFlowDocument.xaml.cs
 *
 * Implements the ConsoleFlowDocument "code-behind" class.
 */

using System;
using RTextNppPlugin.Logging;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace RTextNppPlugin.WpfControls
{
    /// <summary>
    /// Interaction logic for ConsoleFlowDocument.xaml
    /// </summary>
    public partial class ConsoleFlowDocument : FlowDocument, ILoggingObserver, System.IDisposable
    {
        #region Interface

        /**
         * Default constructor.
         */
        public ConsoleFlowDocument()
        {
            InitializeComponent();
            //subscribe to logger singleton
            Logger.Instance.Subscribe(this);
        }



        /**
         * Appends a msg.
         *
         * \param   msg     The message.
         * \param   type    The type.
         */
        public void Append(string msg, Logger.MessageType type)
        {
            switch (type)
            {
                case Logger.MessageType.Info:
                    Append("Info : " + msg, "Information");
                    break;
                case Logger.MessageType.Warning:
                    Append("Warning : " + msg, "Warning");
                    break;
                case Logger.MessageType.Error:
                    Append("Error : " + msg, "Error");
                    break;
                case Logger.MessageType.FatalError:
                    Append("Fatal error : " + msg, "FatalError");
                    break;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Helpers

        private void Append(string msg, string style)
        {
            if (Dispatcher.CheckAccess())
            {
                Debug.Assert(Blocks.LastBlock != null);
                Debug.Assert(Blocks.LastBlock is Paragraph);
                var run = new Run(msg);
                run.Style = (Style)(Resources[style]);
                if (run.Style == null)
                {
                    run.Style = (Style)(Resources["Information"]);
                }
                ((Paragraph)Blocks.LastBlock).Inlines.Add(run);
                ScrollParent(this);                
            }
            else
            {
                Dispatcher.Invoke(new Action<string, string>(Append), msg, style);
            }
        }

        private static void ScrollParent(FrameworkContentElement element)
        {
            if (element != null)
            {
                if (element.Parent is TextBoxBase)
                {
                    ((TextBoxBase)element.Parent).ScrollToEnd();
                }
                else if (element.Parent is ScrollViewer)
                {
                    ((ScrollViewer)element.Parent).ScrollToEnd();
                }
                else
                {
                    ScrollParent(element.Parent as FrameworkContentElement);
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Logger.Instance.Unsubscribe(this);
            }
            _disposed = true;
        }

        #endregion

        #region Data Members
        private bool _disposed;
        private delegate void AppendTextDelegate(string msg, string style);

        #endregion
    }
}
