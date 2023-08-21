using Phoenix.Common.Networking.Channels;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Common.SceneReplication
{
    /// <summary>
    /// Scene Replication Packet Channel
    /// </summary>
    public class SceneReplicationChannel : PacketChannel
    {
        public override PacketChannel Instantiate()
        {
            return new SceneReplicationChannel();
        }

        protected override void MakeRegistry()
        {
            // Scene loading
            RegisterPacket(new LoadScenePacket());
            RegisterPacket(new UnloadScenePacket());

            // Initial replication
            RegisterPacket(new InitialSceneReplicationStartPacket());
            RegisterPacket(new InitialSceneReplicationCompletePacket());

            // Replication
            RegisterPacket(new SpawnPrefabPacket());
            RegisterPacket(new CreateObjectPacket());
            RegisterPacket(new DestroyObjectPacket());
            RegisterPacket(new ReparentObjectPacket());
            RegisterPacket(new ObjectChangeScenePacket());
            RegisterPacket(new ReplicateObjectPacket());
            RegisterPacket(new ComponentMessagePacket());

            // Subscription event packets
            RegisterPacket(new SceneReplicationSubscribeRoomPacket());
            RegisterPacket(new SceneReplicationSubscribeScenePacket());
            RegisterPacket(new SceneReplicationDesubscribeRoomPacket());
            RegisterPacket(new SceneReplicationDesubscribeScenePacket());
            RegisterHandler(new SceneSubscribeHandler());
        }
    }
}
