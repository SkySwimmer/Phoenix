using Phoenix.Common.IO;

namespace Phoenix.Common.Networking.Packets
{
    /// <summary>
    /// Packet interface
    /// </summary>
    public abstract class AbstractNetworkPacket
    {
        /// <summary>
        /// Defines if the packet is length-prefixed or not.<br/>
        /// <br/>
        /// WARNING! While setting this to false allows you to send more than 2 gigabyte in a packet, it will not have any buffering or safties!<br/>
        /// Make sure that all bytes that are sent are also read, else stream corruption will occur!<br/>
        /// </summary>
        public virtual bool LengthPrefixed { get { return true; } }

        /// <summary>
        /// Defines if the packet is synchronized while handling
        /// </summary>
        public virtual bool Synchronized { get { return false; } }

        /// <summary>
        /// Creates a new packet instance
        /// </summary>
        /// <returns>New AbstractNetworkPacket instance</returns>
        public abstract AbstractNetworkPacket Instantiate();

        /// <summary>
        /// Parses the packet
        /// </summary>
        /// <param name="reader">Input data reader</param>
        public abstract void Parse(DataReader reader);

        /// <summary>
        /// Builds the packet
        /// </summary>
        /// <param name="writer">Output data writer</param>
        public abstract void Write(DataWriter writer);
    }
}