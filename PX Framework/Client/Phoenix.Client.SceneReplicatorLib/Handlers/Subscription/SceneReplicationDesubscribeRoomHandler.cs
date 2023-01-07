using Phoenix.Client.Components;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Subscription
{
    public class SceneReplicationDesubscribeRoomHandler : PacketHandler<SceneReplicationDesubscribeRoomPacket>
    {
        protected override PacketHandler<SceneReplicationDesubscribeRoomPacket> CreateInstance()
        {
            return new SceneReplicationDesubscribeRoomHandler();
        }

        protected override bool Handle(SceneReplicationDesubscribeRoomPacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
                client.GetComponent<SceneReplicationComponent>().DesbuscribeRoom(packet.Room);
            return true;
        }
    }
}
