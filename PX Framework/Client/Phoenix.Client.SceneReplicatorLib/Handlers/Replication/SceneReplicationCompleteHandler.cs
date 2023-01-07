using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
{
    public class SceneReplicationCompleteHandler : PacketHandler<SceneReplicationCompletePacket>
    {
        protected override PacketHandler<SceneReplicationCompletePacket> CreateInstance()
        {
            return new SceneReplicationCompleteHandler();
        }

        protected override bool Handle(SceneReplicationCompletePacket packet)
        {
            return true;
        }
    }
}
