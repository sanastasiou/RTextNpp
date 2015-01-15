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

        public void Append(MessageType type, string channel, string msg, params object[] args)
        {
            Append(type, channel, String.Format(msg, args));
        }

        /**
         * Appends a msg to all observers.
         *
         * \param   type    The type.
         * \param   msg     The message.
         * \param   channel The channel.
         */
        private void Append(MessageType type, string channel, string msg)
        {
            msg = DateTime.Now + " : " + msg;
            if(!msg.EndsWith(Environment.NewLine))
            {
                msg += Environment.NewLine;
            }
            lock (_lock)
            {
                //many threads/processes could be accesing this object - mutex needed
                _observers.ForEach(x => x.Append(type, channel, msg));
            }
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
        #endregion
    }
}
