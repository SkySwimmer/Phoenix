using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Common.SceneReplication
{
    /// <summary>
    /// Scene subscribe packet handler, mostly intended as an internal handler for the server to process responses
    /// </summary>
    public class SceneSubscribeHandler : PacketHandler<SceneReplicationSubscribeScenePacket>
    {
        /// <summary>
        /// Packet handler
        /// </summary>
        /// <param name="packet">Packet to handle</param>
        /// <returns>True if handled, false otherwise</returns>
        public delegate bool PacketHandler(SceneReplicationSubscribeScenePacket packet);

        /// <summary>
        /// The actual packet handler, assign this to receive the packet
        /// </summary>
        public PacketHandler? Handler;

        protected override PacketHandler<SceneReplicationSubscribeScenePacket> CreateInstance()
        {
            return new SceneSubscribeHandler()
            {
                Handler = this.Handler
            };
        }

        protected override bool Handle(SceneReplicationSubscribeScenePacket packet)
        {
            if (Handler != null)
                return Handler(packet);
            return false;
        }
    }
}
