using Common.Handlers.Common;
using Common.Packets;
using Phoenix.Common.Networking.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Channels
{
    /// <summary>
    /// Example packet channel
    /// </summary>
    public class ExampleChannel : PacketChannel
    {
        public override PacketChannel Instantiate()
        {
            // Create a instance of our channel
            return new ExampleChannel();
        }

        protected override void MakeRegistry()
        {
            // Register our packets and common handlers
            // Registering handlers here will add them to both client and server


            // Register our packets
            RegisterPacket(new ServersentCommonHelloWorldPacket());
            RegisterPacket(new ExampleRequestPacket());
            RegisterPacket(new ExampleResponsePacket());
            RegisterPacket(new ExampleClientRequestPacket());
            
            // Register the common packet handlers
            // As noted above, this handler will be present on both client and server
            // This is because this channel is shared on both ends
            RegisterHandler(new ServersentCommonHelloWorldHandler());


            // This is important, for the ExampleRequestPacket we dont want a handler here
            // If we add a handler here for that packet, it will handle it on both sides
            //
            // You dont want that.
            // If you are requesting data from the client, and have the request packet handler on this end too
            // it might end up with clients being able to pull sensitive data.
            //
            // Only register handlers in the channel shared between client and server if you are certain you want them on both sides
            //
            // For instance, we also dont want the handler for ExampleClientRequestPacket here as its intended to only be processed by the server
            // Go to SharedGameServer to see how to register server-side handlers


            // Now, here it gets interesting...
            // ExampleResponsePacket does not need a handler on any side
            //
            // This is because this packet will only be sent from the client in response to the ExampleRequestPacket
            // Phoenix will handle the response packet and pass it to the component
            // See ExampleServerComponent for details
        }
    }
}
