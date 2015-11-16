/**
 * \file    Logging\Logger.cs
 *
 * Implements the logger singleton class.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace RTextNppPlugin.Logging
{
    public sealed class Logger : ISubscriber
    {
        #region Data Members
        private static volatile Logger _instance;                                                         //!< Singleton Instance.
        private static object _lock                     = new Object();                                   //!< Mutex.
        private List<ILoggingObserver> _observers;                                                        //!< List of observers.
        private readonly Regex REPLACE_FORMAT_REGEX     = new Regex(@"\{(\d+)\}", RegexOptions.Compiled); //!< Regex to ensure string format is correct.
        private const string REPLACEMENT_FORMAT_STRING  = @"~LEFT_LOL~$1~RIGHT_LOL~";                     //!< String replacement for format correctness.
        private const string LEFT_REPLACEMENT_TOKEN     = @"~LEFT_LOL~";
        private const string RIGHT_REPLACEMENT_TOKEN    = @"~RIGHT_LOL~";
        #endregion
        #region Implementation Details
        private Logger()
        {
            _observers = new List<ILoggingObserver>(10);
        }
        #endregion
        #region Public Inteface
        /**
         * Values that represent MessageType.
         */
        public enum MessageType
        {
            Info,
            Warning,
            Error,
            FatalError
        }
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Logger();
                        }
                    }
                }
                return _instance;
            }
        }
        public void Append(string msg, params object[] args)
        {
            #if DEBUG
            try
            {
                var a = PrepareStringForFormat(msg);
                DoAppend(MessageType.Info, Constants.DEBUG_CHANNEL, String.Format(PrepareStringForFormat(msg), args));
            }
            catch(System.FormatException)
            {
                DoAppend(MessageType.Info, Constants.DEBUG_CHANNEL, msg);
            }
            #endif
        }
        public void Append(MessageType type, string channel, string msg, params object[] args)
        {
            try
            {
                var a = PrepareStringForFormat(msg);
                DoAppend(type, channel, String.Format(PrepareStringForFormat(msg), args));
            }
            catch (System.FormatException)
            {
                DoAppend(type, channel, msg);
            }
        }
        /**
         * Appends a msg to all observers.
         *
         * \param   type    The type.
         * \param   msg     The message.
         * \param   channel The channel.
         */
        private void DoAppend(MessageType type, string channel, string msg)
        {
            if (String.IsNullOrWhiteSpace(msg))
            {
                return;
            }
            msg = DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second + ": " + msg;
            if (!msg.EndsWith(Environment.NewLine))
            {
                msg += Environment.NewLine;
            }
            _observers.ForEach(x => x.Append(type, channel, msg));
        }
        public void Subscribe(ILoggingObserver obs)
        {
            lock (_lock)
            {
                if (obs != null)
                {
                    _observers.Add(obs);
                }
            }
        }
        public void Unsubscribe(ILoggingObserver obs)
        {
            lock (_lock)
            {
                if (obs != null)
                {
                    _observers.Remove(obs);
                }
            }
        }

        private string PrepareStringForFormat(string unformattedString)
        {
            var aTemp = REPLACE_FORMAT_REGEX.Replace(unformattedString, REPLACEMENT_FORMAT_STRING);
            return aTemp.Replace("{", "{{").Replace("}", "}}").Replace(LEFT_REPLACEMENT_TOKEN, "{").Replace(RIGHT_REPLACEMENT_TOKEN, "}");
        }

        #endregion
    }
}