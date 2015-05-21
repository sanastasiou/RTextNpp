namespace RTextNppPlugin.RText.StateEngine
{
    public interface IConnectorState
    {
        ConnectorStates State { get; }

        void ExecuteCommand(Command command);

        void OnEntry();

        void OnExit(ConnectorStates newState);
    }
}
