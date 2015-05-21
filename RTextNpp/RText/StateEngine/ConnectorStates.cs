using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTextNppPlugin.RText.StateEngine
{
    public enum ConnectorStates
    {
        Disconnected,
        Connecting,
        Loading,
        Busy,
        Idle
    }

    class Disconnected : IConnectorState
    {
        #region [Data Members]
	    private readonly IConnector _connector = null;
        private const ConnectorStates _state = ConnectorStates.Disconnected;
	    #endregion

        #region IConnectorState Members

        public ConnectorStates State { get { return _state; } }

        public Disconnected(IConnector connector)
        {
            _connector = connector;
            OnEntry();
        }

        public void ExecuteCommand(Command command)
        {
            switch (command)
            {
                case Command.Connect:
                    OnExit(ConnectorStates.Connecting);
                    _connector.CurrentState = new Connecting(_connector);
                    _connector.CurrentState.ExecuteCommand(Command.Connect);
                    break;
                default:
                    _connector.CurrentState = this;
                    break;
            }

        }

        public void OnEntry()
        {
            _connector.OnDisconnectedEntry();
        }

        public void OnExit(ConnectorStates newState)
        {
            _connector.OnStateLeft(_state, newState);
        }

        #endregion
    }

    class Connecting : IConnectorState
    {
        #region [Data Members]
        private readonly IConnector _connector = null;
        private const ConnectorStates _state = ConnectorStates.Connecting;
        #endregion

        #region IConnectorState Members

        public ConnectorStates State { get { return _state; } }

        public Connecting(IConnector connector)
        {
            _connector = connector;
        }

        public void ExecuteCommand(Command command)
        {
            switch (command)
            {
                case Command.Connect:
                    OnEntry();
                    break;
                case Command.Connected:
                    _connector.CurrentState = new Loading(_connector);
                    _connector.CurrentState.ExecuteCommand(Command.LoadModel);
                    OnExit(ConnectorStates.Loading);
                    break;
                default:
                    OnExit(ConnectorStates.Disconnected);
                    _connector.CurrentState = new Disconnected(_connector);
                    break;
            }
        }        

        public void OnEntry()
        {
            _connector.OnConnectingEntry();
        }

        public void OnExit(ConnectorStates newState)
        {
            _connector.OnStateLeft(_state, newState);
        }

        #endregion
    }
   
    class Loading : IConnectorState
    {
        #region [Data Members]
        private readonly IConnector _connector = null;
        private const ConnectorStates _state = ConnectorStates.Loading;
        #endregion

        #region IConnectorState Members

        public ConnectorStates State { get { return _state; } }

        public Loading(IConnector connector)
        {
            _connector = connector;
        }

        public void ExecuteCommand(Command command)
        {
            switch (command)
            {
                case Command.ExecuteFinished:
                    OnExit(ConnectorStates.Idle);
                    _connector.CurrentState = new Idle(_connector);
                    break;
                case Command.Disconnected:
                    OnExit(ConnectorStates.Disconnected);
                    _connector.CurrentState = new Disconnected(_connector);
                    break;
                default:
                    OnEntry();
                    _connector.CurrentState = this;
                    break;
            }
        }

        public void OnEntry()
        {
            _connector.OnLoadingEntry();
        }

        public void OnExit(ConnectorStates newState)
        {
            _connector.OnStateLeft(_state, newState);
        }

        #endregion
    }

    class Idle : IConnectorState
    {
        #region [Data Members]
        private readonly IConnector _connector = null;
        private const ConnectorStates _state = ConnectorStates.Idle;
        #endregion

        #region IConnectorState Members

        public ConnectorStates State { get { return _state; } }

        public Idle(IConnector connector)
        {
            _connector = connector;
        }

        public void ExecuteCommand(Command command)
        {
            switch (command)
            {
                case Command.Execute:
                    OnExit(ConnectorStates.Busy);
                    _connector.CurrentState = new Busy(_connector);
                    break;
                case Command.Disconnected:
                    OnExit(ConnectorStates.Disconnected);
                    _connector.CurrentState = new Disconnected(_connector);
                    break;
                case Command.LoadModel:
                    OnExit(ConnectorStates.Loading);
                    _connector.CurrentState = new Loading(_connector);
                    break;
                default:
                    _connector.CurrentState = this;
                    break;
            }
        }

        public void OnEntry()
        {
        }

        public void OnExit(ConnectorStates newState)
        {
            _connector.OnStateLeft(_state, newState);
        }

        #endregion
    }

    class Busy : IConnectorState
    {
        #region [Data Members]
        private readonly IConnector _connector = null;
        private const ConnectorStates _state = ConnectorStates.Busy;
        #endregion

        #region IConnectorState Members

        public ConnectorStates State { get { return _state; } }

        public Busy(IConnector connector)
        {
            _connector = connector;
            OnEntry();
        }

        public void ExecuteCommand(Command command)
        {
            switch (command)
            {
                case Command.ExecuteFinished:
                    OnExit(ConnectorStates.Idle);
                    _connector.CurrentState = new Idle(_connector);
                    break;
                case Command.Disconnected:
                    OnExit(ConnectorStates.Disconnected);
                    _connector.CurrentState = new Disconnected(_connector);
                    break;
                default:
                    _connector.CurrentState = this;
                    break;
            }
        }

        public void OnEntry()
        {
        }

        public void OnExit(ConnectorStates newState)
        {
            _connector.OnStateLeft(_state, newState);
        }

        #endregion
    }
}
