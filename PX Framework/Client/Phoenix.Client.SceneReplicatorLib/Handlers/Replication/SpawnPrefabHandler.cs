using Phoenix.Client.Components;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
{
    public class SpawnPrefabHandler : PacketHandler<SpawnPrefabPacket>
    {
        protected override PacketHandler<SpawnPrefabPacket> CreateInstance()
        {
            return new SpawnPrefabHandler();
        }

        protected override bool Handle(SpawnPrefabPacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.IsSubscribedToScene(packet.ScenePath) && comp.IsSubscribedToRoom(packet.Room) && comp.Bindings != null)
                {
                    comp.Bindings.RunOnNextFrameUpdate(() =>
                    {
                        comp.GetLogger().Trace("Spawning prefab " + packet.PrefabPath + " in scene " + packet.ScenePath + " of room " + packet.Room + ", parent object: " + (packet.ParentObjectID == null ? "<root object>" : packet.ParentObjectID) + "...");
                        comp.Bindings.SpawnPrefab(packet);
                    });
                }
            }
            return true;
        }
    }
}
