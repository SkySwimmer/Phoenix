using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
{
    public class SceneReplicationStartHandler : PacketHandler<SceneReplicationStartPacket>
    {
        protected override PacketHandler<SceneReplicationStartPacket> CreateInstance()
        {
            return new SceneReplicationStartHandler();
        }

        protected override bool Handle(SceneReplicationStartPacket packet)
        {
            return true;
        }
    }
}
