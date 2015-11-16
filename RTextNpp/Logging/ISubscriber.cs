/**
 * \file    Logging\ISubscriber.cs
 *
 * Declares the ISubscriber interface.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RTextNppPlugin.Logging
{
    interface ISubscriber
    {
        void Subscribe(ILoggingObserver obs);
        void Unsubscribe(ILoggingObserver obs);
    }
}