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

namespace RTextNppPlugin.Logging
{
    public sealed class Logger : ISubscriber
    {
        #region Data Members
        private static volatile Logger _instance;  //!< Singleton Instance.
        private static object _lock = new Object();//!< Mutex.
        private List<ILoggingObserver> _observers; //!< List of observers.
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
                            _instance = new Logger();
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
                DoAppend(MessageType.Info, Constants.DEBUG_CHANNEL, String.Format(msg, args));
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
                DoAppend(type, channel, String.Format(msg, args));
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

        /**
         * A DateTime extension method that truncates time to msec, s etc.
         *
         * \param   dateTime    The dateTime to act on.
         * \param   timeSpan    The time span.
         *
         * \return  A DateTime.
         */
        private DateTime Truncate(DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
        #endregion
    }
}
