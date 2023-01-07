using Phoenix.Client.Components;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Subscription
{
    public class SceneReplicationDesubscribeSceneHandler : PacketHandler<SceneReplicationDesubscribeScenePacket>
    {
        protected override PacketHandler<SceneReplicationDesubscribeScenePacket> CreateInstance()
        {
            return new SceneReplicationDesubscribeSceneHandler();
        }

        protected override bool Handle(SceneReplicationDesubscribeScenePacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
                client.GetComponent<SceneReplicationComponent>().DesubscribeScene(packet.ScenePath);
            return true;
        }
    }
}
