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
    public sealed class Logger : ILoggingObserver, ISubscriber
    {
        #region Data Members
        private static volatile Logger _instance;  //!< Singleton Instance.
        private static object _lock = new Object();//!< Mutex.
        private List<ILoggingObserver> _observers; //!< List of observers.

        //todo need to save messages from various workspaces... we only have one document..
        //private ConcurrentQueue<Tuple<string, MessageType>> _msgList = new ConcurrentQueue<Tuple<string, MessageType>>();
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

        public void Append(string msg, MessageType type, params object[] args)
        {
            Append(String.Format(msg, args), type);
        }

        /**
         * Appends a msg to all observers.
         *
         * \param   msg     The message.
         * \param   type    The type.
         */
        public void Append(string msg, MessageType type)
        {
            lock (_lock)
            {
                //many threads/process could be accesing this object - mutex needed
                _observers.ForEach(x => x.Append(msg, type));
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
