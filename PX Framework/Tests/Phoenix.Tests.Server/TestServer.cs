using Phoenix.Common.IO;
using Phoenix.Common.Networking.Registry;
using Phoenix.Debug;
using Phoenix.Server;
using Phoenix.Server.Packages;
using Phoenix.Server.NetworkServerLib;
using System.Text;
using Phoenix.Server.Components;
using Phoenix.Common;
using Phoenix.Common.SceneReplication;

namespace Phoenix.Tests.Server
{
    public class TestServer : PhoenixDedicatedServer
    {
        public override void Prepare()
        {
        }

        public override bool SupportMods()
        {
            return true;
        }

        protected override void SetupServers()
        {
            // Packet registry
            ChannelRegistry registry = new ChannelRegistry();
            registry.Register(new SceneReplicationChannel());
            registry.Register(new TestChannel());

            // Game server
            GameServer testServer = new GameServer("test");
            testServer.AddComponentPackage(new CorePackage());
            testServer.AddComponent(new TestComponent());
            if (Game.DebugMode)
                testServer.AddComponent(new NetworkServerComponent());
            testServer.AddComponent(new ServerListPublisherComponent());
            testServer.AddComponent(new PlayerManagerComponent());
            testServer.AddComponent(new AuthenticationManagerComponent());
            testServer.AddComponent(new SceneReplicationComponent());

            // Protocol version and registry
            testServer.ProtocolVersion = 1;
            testServer.ChannelRegistry = registry;

            // Add it
            AddServer(testServer);
        }
    }
}
