namespace Phoenix.Server.Components.SceneReplication.Old.Objects.ReplicationQueue
{
    public class ObjectDestroyDataframe : ReplicationDataframe
    {
        public override ReplicationCommandType Type => ReplicationCommandType.DESTROY;
    }
}
