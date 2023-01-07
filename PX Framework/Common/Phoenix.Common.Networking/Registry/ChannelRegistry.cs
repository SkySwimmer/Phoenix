using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Channels;

namespace Phoenix.Common.Networking.Registry
{
    /// <summary>
    /// Packet and channel registry
    /// </summary>
    public class ChannelRegistry
    {
        private List<PacketChannel> channels = new List<PacketChannel>();

        /// <summary>
        /// Registers a packet channel
        /// </summary>
        /// <param name="channel">Channel to register</param>
        public void Register(PacketChannel channel)
        {
            Logger.GetLogger("channel-registry").Trace("Registering packet channel: " + channel.GetType().FullName);
            Logger.GlobalMessagePrefix += "  ";
            channel.CallMakeRegistry();
            Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
            channels.Add(channel);
        }

        /// <summary>
        /// Retrieves packet channels
        /// </summary>
        /// <typeparam name="T">Channel type</typeparam>
        /// <returns>Channel instance</returns>
        public T GetChannel<T>() where T : PacketChannel
        {
            foreach (PacketChannel ch in channels)
            {
                if (ch is T)
                    return (T)ch;
            }
            throw new ArgumentException("Channel not found");
        }

        /// <summary>
        /// Retrieves all registered channels
        /// </summary>
        public PacketChannel[] Channels
        {
            get
            {
                return channels.ToArray();
            }
        }
    }
}
