using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTextNppPlugin.RText.StateEngine;

namespace RTextNppPlugin.RText
{
    interface IConnector
    {
        void OnDisconnectedEntry();

        void OnConnectingEntry();

        void OnStateLeft(ConnectorStates oldState, ConnectorStates newState);

        IConnectorState CurrentState { get; set; }

        void OnLoadingEntry();
    }
}
