using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Client.IntegratedServerBootstrapper;
using Phoenix.Server;

namespace Common
{
    /// <summary>
    /// Example integrated server
    /// </summary>
    public class ExampleIntegratedServer : PhoenixIntegratedServer
    {
        protected override void Prepare()
        {
            // Called before the server is started
        }

        protected override GameServer SetupServer()
        {
            // Create a new server, using our shared base, and adding the integrated server components to it
            GameServer srv1 = SharedGameServer.CreateServer("server");
            
            // Add integrated server components here
            // Note that integrated servers, unless a configuration management component is added, will have a memory-based configuration manager

            return srv1;
        }
    }
}
