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
        private static volatile Logger instance;
        private static object syncRoot = new Object();
        private List<ILoggingObserver> _observers;

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
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Logger();
                    }
                }

                return instance;
            }
        }

        /**
         * Appends a msg to all observers.
         *
         * \param   msg     The message.
         * \param   type    The type.
         */
        public void Append(string msg, MessageType type)
        {
            _observers.ForEach(x => x.Append(msg, type));
        }

        public void Subscribe(ILoggingObserver obs)
        {
            if(obs != null)
            {
                _observers.Add(obs);
            }
        }

        public void Unsubscribe(ILoggingObserver obs)
        {
            if(obs != null)
            {
                _observers.Remove(obs);
            }
        }
        #endregion
    }
}
