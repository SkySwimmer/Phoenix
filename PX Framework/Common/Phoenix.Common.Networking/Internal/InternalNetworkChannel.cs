using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.Networking.Internal
{
    public abstract class InternalPacketHandler
    {
        public abstract InternalPacketHandler Instantiate();

        /// <summary>
        /// Checks if this packet handler can handle the given packet
        /// </summary>
        public virtual bool CanHandle(AbstractNetworkPacket packet)
        {
            return true;
        }

        /// <summary>
        /// Handles the packet
        /// </summary>
        public abstract bool Handle(AbstractNetworkPacket packet, PacketChannel channel);
    }

}
