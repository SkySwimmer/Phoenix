using Phoenix.Client.IntegratedServerBootstrapper;
using Phoenix.Common.Networking.Registry;
using Phoenix.Common.SceneReplication;
using Phoenix.Server;
using Phoenix.Server.Components;
using Phoenix.Server.Packages;
using System;

namespace Phoenix.Tests.Server
{
    public class TestIntegratedServer : PhoenixIntegratedServer
    {
        protected override void Prepare()
        {
        }

        protected override GameServer SetupServer()
        {
            // Packet registry
            ChannelRegistry registry = new ChannelRegistry();
            registry.Register(new SceneReplicationChannel());
            registry.Register(new TestChannel());

            // Game server
            GameServer testServer = new GameServer("test");
            testServer.AddComponent(new TestComponent());
            testServer.AddComponent(new TaskManagerComponent());
            testServer.AddComponent(new PlayerManagerComponent());
            testServer.AddComponent(new AuthenticationManagerComponent());
            testServer.AddComponent(new SceneReplicationComponent());

            // Protocol version and registry
            testServer.ProtocolVersion = 1;
            testServer.ChannelRegistry = registry;

            // Return
            return testServer;
        }
    }
}
