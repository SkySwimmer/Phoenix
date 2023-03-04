namespace Phoenix.Server.Components.SceneReplication.Old.Objects.ReplicationQueue
{
    public class ReplicationCommand
    {
        public string Room;
        public string Scene;
        public string? ObjectPath;
        public ReplicationCommandType Type;
        public object? Data;
    }

    public enum ReplicationCommandType
    {
        DESTROY,
        REPLICATE,
        REPARENT,
        CHANGE_SCENE,
        SPAWN_PREFAB
    }
}
