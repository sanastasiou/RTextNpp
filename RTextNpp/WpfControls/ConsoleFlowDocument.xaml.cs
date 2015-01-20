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
using System.Collections.Generic;
using System.ComponentModel;

namespace RTextNppPlugin.WpfControls
{
    /**
     * Interaction logic for ConsoleFlowDocument.xaml.
     */    
    public partial class ConsoleFlowDocument : FlowDocument, ILoggingObserver, System.IDisposable, INotifyPropertyChanged
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

        public string Channel
        {
            get { return GetValue(ChannelProperty).ToString(); }
            set { SetValue(ChannelProperty, value); }
        }

        public static readonly DependencyProperty ChannelProperty = DependencyProperty.Register("Channel",
                                                                                                typeof(string),
                                                                                                typeof(ConsoleFlowDocument),
                                                                                                new PropertyMetadata(string.Empty, OnChannelPropertyChanged)
                                                                                               );

        /**
         * Appends a msg.
         *
         * \param   msg     The message.
         * \param   type    The type.
         * \param   channel The output channel.
         */
        public void Append(Logger.MessageType type, string channel, string msg)
        {
            switch (type)
            {
                case Logger.MessageType.Info:
                    Append("Info: " + msg, "Information", channel);
                    break;
                case Logger.MessageType.Warning:
                    Append("Warning: " + msg, "Warning", channel);
                    break;
                case Logger.MessageType.Error:
                    Append("Error: " + msg, "Error", channel);
                    break;
                case Logger.MessageType.FatalError:
                    Append("Fatal error: " + msg, "FatalError", channel);
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

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Append(string msg, string style, string channel)
        {
            if (Dispatcher.CheckAccess())
            {
                if (!msg.EndsWith("\r\n") || !msg.EndsWith("\n"))
                {
                    msg += Environment.NewLine;
                }
                if (channel != _currentChannel)
                {
                    //add the entries internally don't log to output
                    if (!_logOutput.ContainsKey(channel))
                    {
                        _logOutput.Add(channel, new List<Run>());
                    }
                    var run = new Run(msg);
                    run.Style = (Style)(Resources[style]);
                    if (run.Style == null)
                    {
                        run.Style = (Style)(Resources["Information"]);
                    }
                    _logOutput[channel].Add(run);
                }
                else
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
                    _logOutput[_currentChannel].Add(run);
                    ScrollParent(this);
                }
            }
            else
            {
                Dispatcher.Invoke(new Action<string, string, string>(Append), msg, style, channel);
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

        private static void OnChannelPropertyChanged(DependencyObject dependencyObject,
               DependencyPropertyChangedEventArgs e)
        {
            ConsoleFlowDocument myUserControl = dependencyObject as ConsoleFlowDocument;
            myUserControl.OnPropertyChanged("Channel");
            myUserControl.OnChannelPropertyChanged(e);
        }

        /**
         * Occurs when a user selects a different output channel from the console view.
         *
         * \param   e   Event information to send to registered event handlers.
         */
        private void OnChannelPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(Channel))
            {
                return;
            }
            if (Channel != _currentChannel)
            {
                //channel changed, clear document and update it with "previous" statements, if any
                _currentChannel = Channel;
                ((Paragraph)Blocks.LastBlock).Inlines.Clear();
                //does channel already exist?
                if (_logOutput.ContainsKey(_currentChannel))
                {
                    var lol = _logOutput[_currentChannel];
                    ((Paragraph)Blocks.LastBlock).Inlines.Clear();
                    //load output with previous entries...
                    ((Paragraph)Blocks.LastBlock).Inlines.AddRange(_logOutput[_currentChannel]);
                    ScrollParent(this);
                }
                else
                {
                    _logOutput.Add(_currentChannel, new List<Run>());
                }
            }
        }

        #endregion

        #region Data Members
        private bool _disposed;                                                                 //!< Whether the object has already been disposed.
        private Dictionary<string, List<Run>> _logOutput = new Dictionary<string, List<Run>>(); //!< Holds list of output per channel.
        private string _currentChannel = null;                                                  //!< Holds the current channel.

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
