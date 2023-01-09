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
    /// Example request packet
    /// </summary>
    public class ExampleRequestPacket : AbstractNetworkPacket
    {
        // Defaults
        public string RequestString = "Nothing";

        public override AbstractNetworkPacket Instantiate()
        {
            // Create our packet instance
            // This is used by the packet processor to de-serialize packets
            return new ExampleRequestPacket();
        }

        public override void Parse(DataReader reader)
        {
            // Called by Phoenix to load the packet data
            // This is how your packet is de-serialized
            RequestString = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            // Called by Phoenix to write the packet data
            // This is how your packet is serialized
            //
            // Read the ServersentCommonHelloWorldPacket example for details
            writer.WriteString(RequestString);
        }
    }
}
