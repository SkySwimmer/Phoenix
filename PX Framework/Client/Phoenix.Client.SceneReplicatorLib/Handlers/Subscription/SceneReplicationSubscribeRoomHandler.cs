using Phoenix.Client.Components;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Subscription
{
    public class SceneReplicationSubscribeRoomHandler : PacketHandler<SceneReplicationSubscribeRoomPacket>
    {
        protected override PacketHandler<SceneReplicationSubscribeRoomPacket> CreateInstance()
        {
            return new SceneReplicationSubscribeRoomHandler();
        }

        protected override bool Handle(SceneReplicationSubscribeRoomPacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
                client.GetComponent<SceneReplicationComponent>().SubscribeRoom(packet.Room);
            return true;
        }
    }
}
