using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Phoenix.Server;
using Phoenix.Server.Components;

namespace Server
{
    /// <summary>
    /// Dedicated Server Startup Class
    /// </summary>
    public class DedicatedServer : PhoenixDedicatedServer
    {
        public override void Prepare()
        {
            // Called after loading the assemblies
        }

        public override bool SupportMods()
        {
            // True to enable mod support, false otherwise
            return false;
        }

        protected override void SetupServers()
        {
            // Set up the server instances

            // Create a new server, using our shared base, and adding the dedicated server components to it
            GameServer srv1 = SharedGameServer.CreateServer("server");
            srv1.AddComponent(new ConfigManagerComponent()); // Add the file-based configuration manager
            srv1.AddComponent(new ServerListPublisherComponent()); // Add the server list component
            AddServer(srv1); // Add the server
        }
    }
}
