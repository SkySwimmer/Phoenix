using Phoenix.Client.Components;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Subscription
{
    public class SceneReplicationSubscribeSceneHandler : PacketHandler<SceneReplicationSubscribeScenePacket>
    {
        protected override PacketHandler<SceneReplicationSubscribeScenePacket> CreateInstance()
        {
            return new SceneReplicationSubscribeSceneHandler();
        }

        protected override bool Handle(SceneReplicationSubscribeScenePacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();

                // Wait for scene to load if the client is still loading it
                while (comp.IsSceneLoading(packet.ScenePath))
                    Thread.Sleep(10);

                // Check status
                if (!comp.IsSceneLoaded(packet.ScenePath))
                {
                    // Send failure
                    packet.Success = true;
                    GetChannel().SendPacket(packet);
                }
                else
                {
                    // Subscribe
                    comp.CompleteSubscribeScene(packet.Room, packet.ScenePath);

                    // Send success
                    packet.Success = true;
                    GetChannel().SendPacket(packet);
                }
            }
            return true;
        }
    }
}
