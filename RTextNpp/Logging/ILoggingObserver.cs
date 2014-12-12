/**
 * \file    Logging\ILoggingObserver.cs
 *
 * Declares the ILoggingObserver interface.
 */

namespace RTextNppPlugin.Logging
{
    public interface ILoggingObserver
    {
        /**
         * Appends a msg.
         *
         * \param   msg     The message.
         * \param   type    The type.
         * \param   channel The output channel.
         */
        void Append(string msg, Logger.MessageType type);
    }
}
