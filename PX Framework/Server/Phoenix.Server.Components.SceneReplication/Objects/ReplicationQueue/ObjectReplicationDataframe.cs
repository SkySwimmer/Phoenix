using Phoenix.Server.SceneReplication.Coordinates;

namespace Phoenix.Server.Components.SceneReplication.Objects.ReplicationQueue
{
    public class ObjectReplicationDataframe : ReplicationDataframe
    {
        public bool HasTransformChanges = false;
        public bool HasNameChanges = false;
        public bool HasActiveStatusChanges = false;
        public bool HasDataChanges = false;

        public string? Name;
        public Transform? Transform;
        public bool Active;
        public List<string> RemovedData = new List<string>();
        public Dictionary<string, object> Data = new Dictionary<string, object>();

        public override ReplicationCommandType Type => ReplicationCommandType.REPLICATE;
    }
}
