using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Channels;
using Common.Components.Server;
using Common.Handlers.Server;
using Phoenix.Server;
using Phoenix.Server.Components;

namespace Common
{
    /// <summary>
    /// Shared game server stuff
    /// </summary>
    public static class SharedGameServer
    {
        /// <summary>
        /// Creates a game server without any dedicated or integrated specifics, only game code that is shared
        /// </summary>
        /// <param name="logID">Log ID</param>
        /// <returns>GameServer instance</returns>
        public static GameServer CreateServer(string logID)
        {
            // Create server
            GameServer server = new GameServer(logID);



            // Add the registry to the server
            server.ChannelRegistry = SharedChannelRegistry.Create();

            // Add server-side packet handlers
            ExampleChannel channel = server.ChannelRegistry.GetChannel<ExampleChannel>();

            // Register the handler for ExampleClientRequestPacket
            channel.RegisterHandler(new ExampleClientRequestHandler());



            // Assign protocol version
            // This is our game protocol version tied to our program version
            // Change this number each time you add/remove packets, change things related to scene replication or when you do a major game update
            //
            // This protocol version is used to check if clients are compatible or not
            // Currently its protocol version 1
            server.ProtocolVersion = 1;



            // Add some basic components used on both dedicated and integrated servers
            server.AddComponent(new TaskManagerComponent()); // Task manager
            server.AddComponent(new PlayerManagerComponent()); // Player manager
            server.AddComponent(new AuthenticationManagerComponent()); // Player authenticator
            server.AddComponent(new SceneReplicationComponent()); // Scene replication

            // Add program-specific components
            server.AddComponent(new ExampleServerComponent());
            


            // Return it
            return server;
        }
    }
}
