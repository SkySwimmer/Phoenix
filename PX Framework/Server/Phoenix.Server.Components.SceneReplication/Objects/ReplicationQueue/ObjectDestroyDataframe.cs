namespace Phoenix.Server.Components.SceneReplication.Objects.ReplicationQueue
{
    public class ObjectDestroyDataframe : ReplicationDataframe
    {
        public override ReplicationCommandType Type => ReplicationCommandType.DESTROY;
    }
}
