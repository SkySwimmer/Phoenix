namespace Phoenix.Server.Components.SceneReplication.Old.Objects.ReplicationQueue
{
    public abstract class ReplicationDataframe
    {
        public string Room;
        public string ScenePath;
        public string? ObjectPath;

        public abstract ReplicationCommandType Type { get; }
    }
}
