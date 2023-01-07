namespace Phoenix.Server.Components.SceneReplication.Objects.ReplicationQueue
{
    public class ObjectSceneChangeDataframe : ReplicationDataframe
    {
        public string? NewScenePath;

        public override ReplicationCommandType Type => ReplicationCommandType.CHANGE_SCENE;
    }
}
