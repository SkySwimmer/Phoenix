using Common.Packets;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Handlers.Common
{
    /// <summary>
    /// Shared packet handler for ServersentCommonHelloWorldPacket, present on both client and server
    /// </summary>
    public class ServersentCommonHelloWorldHandler : PacketHandler<ServersentCommonHelloWorldPacket>
    {
        protected override PacketHandler<ServersentCommonHelloWorldPacket> CreateInstance()
        {
            // Create a instance of our handler
            return new ServersentCommonHelloWorldHandler();
        }

        protected override bool Handle(ServersentCommonHelloWorldPacket packet)
        {
            // Log the message
            Logger.GetLogger("example").Info(packet.Message1 + " " + packet.Message2);

            return true; // Successfully handled
        }
    }
}
