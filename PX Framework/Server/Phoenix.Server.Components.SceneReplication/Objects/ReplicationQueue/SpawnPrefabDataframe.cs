namespace Phoenix.Server.Components.SceneReplication.Objects.ReplicationQueue
{
    public class SpawnPrefabDataframe : ReplicationDataframe
    {
        public string PrefabPath;
        public override ReplicationCommandType Type => ReplicationCommandType.SPAWN_PREFAB;
    }
}
