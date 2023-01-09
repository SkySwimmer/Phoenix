using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Common.Packets
{
    /// <summary>
    /// Simple message packet
    /// </summary>
    public class ServersentCommonHelloWorldPacket : AbstractNetworkPacket
    {
        // Defaults
        public string Message1 = "No";
        public string Message2 = "Data";

        public override AbstractNetworkPacket Instantiate()
        {
            // Create our packet instance
            // This is used by the packet processor to de-serialize packets
            return new ServersentCommonHelloWorldPacket();
        }

        public override void Parse(DataReader reader)
        {
            // Called by Phoenix to load the packet data
            // This is how your packet is de-serialized
            Message1 = reader.ReadString();
            Message2 = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            // Called by Phoenix to write the packet data
            // This is how your packet is serialized
            //
            // Packet reading/writing is done in order
            // Each field is written and read in the same order
            //
            // Ultimately, the data is written in binary
            // So make sure the parser and writer both have the same format
            writer.WriteString(Message1);
            writer.WriteString(Message2);
        }
    }
}
