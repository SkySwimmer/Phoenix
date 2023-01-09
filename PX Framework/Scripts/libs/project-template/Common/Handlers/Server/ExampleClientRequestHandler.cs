using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.Logging;
using Common.Packets;
using Phoenix.Server.Players;

namespace Common.Handlers.Server
{
    /// <summary>
    /// Server-side handler for the ExampleClientRequestPacket, only present on server
    /// </summary>
    public class ExampleClientRequestHandler : PacketHandler<ExampleClientRequestPacket>
    {
        protected override PacketHandler<ExampleClientRequestPacket> CreateInstance()
        {
            // Create a instance of our handler
            return new ExampleClientRequestHandler();
        }

        protected override bool Handle(ExampleClientRequestPacket packet)
        {
            // Called on the server when the client sends ExampleClientRequestPacket

            // Lets get the player object
            Player? plr = GetChannel().Connection.GetObject<Player>();
            if (plr != null)
            {
                // Now, lets handle this packet
                // Lets send 'Hello <playername>' back
                ExampleResponsePacket response = new ExampleResponsePacket();
                response.ResponseMessage = "Hello " + plr.DisplayName;
                GetChannel().SendPacket(response); // Sends a packet back
            }

            return true;
        }
    }
}
