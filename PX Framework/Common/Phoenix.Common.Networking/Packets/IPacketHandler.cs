using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Internal;

namespace Phoenix.Common.Networking.Packets
{
    /// <summary>
    /// Packet handler abstract
    /// </summary>
    /// <typeparam name="T">Packet type</typeparam>
    public abstract class PacketHandler<T> : InternalPacketHandler where T : AbstractNetworkPacket
    {
        private PacketChannel channel;

        public override InternalPacketHandler Instantiate()
        {
            return CreateInstance();
        }

        public override bool CanHandle(AbstractNetworkPacket packet)
        {
            if (packet is T)
            {
                return CanHandle((T)packet);
            }
            return false;
        }

        public override bool Handle(AbstractNetworkPacket packet, PacketChannel channel)
        {
            if (packet is T)
            {
                this.channel = channel;
                return Handle((T)packet);
            }
            return false;
        }

        /// <summary>
        /// Creates a new instance of this packet handler
        /// </summary>
        protected abstract PacketHandler<T> CreateInstance();

        /// <summary>
        /// Retrieves the packet channel
        /// </summary>
        /// <returns>PacketChannel instance</returns>
        protected PacketChannel GetChannel()
        {
            return channel;
        }

        /// <summary>
        /// Checks if this packet handler can handle the given packet
        /// </summary>
        /// <param name="packet">Packet to check</param>
        /// <returns>True if the packet can be handled by this handler, false otherwise</returns>
        public virtual bool CanHandle(T packet)
        {
            return true;
        }

        /// <summary>
        /// Handles the packet
        /// </summary>
        /// <param name="packet">Packet to handle</param>
        /// <returns>True if the packet was handled, false otherwise</returns>
        protected abstract bool Handle(T packet);
    }
}
