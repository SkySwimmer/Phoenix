using System;
using System.Net;
using System.Net.Sockets;
using Phoenix.Common.Certificates;
using Phoenix.Common.Events;
using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Impl;
using Phoenix.Common.Networking.Registry;
using Phoenix.Server;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Phoenix.Server.Packages;
using Phoenix.Common;

namespace Phoenix.Tests.Server {
    public class Program : IEventListenerContainer {
        public static void Main(string[] args) {
            Console.WriteLine("Phoenix Library Tests: Generic Server Test is starting...");
            Logger.GlobalLogLevel = LogLevel.DEBUG;

            // Create channel registry
            ChannelRegistry registry = new ChannelRegistry();
            registry.Register(new TestChannel());

            // Create connections
            ConnectionBundle bundle = Connections.CreateIntegratedConnections(registry);
            Connection client = bundle.Client;
            ServerConnection server = bundle.Server;
            server.Open();

            // Attach events
            client.Connected += (t, a) => {
                Logger.GetLogger("TEST").Info("Server connection established");
            };
            server.Connected += (t, a) => {
                Logger.GetLogger("TEST").Info("Client connected");
            };
            client.Disconnected += (t, r, a) => {
                Logger.GetLogger("TEST").Info("Server connection closed");
            };
            server.Disconnected += (t, r, a) => {
                Logger.GetLogger("TEST").Info("Client disconnected");
            };

            // Connect
            client.Open();

            // Send test packet
            TestChannel ch = client.GetChannel<TestChannel>();
            ch.SendPacket(new TestPacket()
            {
                Sender = "Phoenix Test Server",
                Message = "Hello World"
            });

            // Disconnect
            server.Close();

            // Test networked
            Connection serverConn = Connections.CreateNetworkServer(12345, registry, null);
            Connection connClient = Connections.CreateNetworkClient("127.0.0.1", 12345, registry, null);

            // Start
            serverConn.Open();
            connClient.Open();

            // Attach events
            connClient.Connected += (t, a) => {
                Logger.GetLogger("TEST").Info("Server connection established");
            };
            serverConn.Connected += (t, a) => {
                Logger.GetLogger("TEST").Info("Client connected");
            };
            connClient.Disconnected += (t, r, a) => {
                Logger.GetLogger("TEST").Info("Server connection closed");
            };
            serverConn.Disconnected += (t, r, a) => {
                Logger.GetLogger("TEST").Info("Client disconnected");
            };

            // Send test packet
            ch = connClient.GetChannel<TestChannel>();
            ch.SendPacket(new TestPacket()
            {
                Sender = "Phoenix Test Server",
                Message = "Hello World"
            });

            // Ping tests
            Logger log = Logger.GetLogger("TEST");
            for (int i = 0; i < 10; i++)
            {
                PingPacket ping = new PingPacket();
                long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                PongPacket? pong = ch.SendPacketAndWaitForResponse<PongPacket>(ping);
                log.Info("Time: " + (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start));
            }

            // Disconnect
            connClient.Close();
            serverConn.Close();
        }
    }

    class GameImpl : Game
    {
        public void Register()
        {
            Implementation = this;
        }

        public override string GetAssetsFolder()
        {
            return "Assets";
        }

        public override string GetDevelopmentStage()
        {
            return "Alpha";
        }

        public override string GetGameFiles()
        {
            return "Assets";
        }

        public override string GetGameID()
        {
            return "test";
        }

        public override string GetPlayerData()
        {
            return "Playerdata";
        }

        public override string GetSessionToken()
        {
            return null;
        }

        public override string GetTitle()
        {
            return "Test Project";
        }

        public override string GetVersion()
        {
            return "1.0.0";
        }

        public override bool HasOfflineSupport()
        {
            return false;
        }

        public override bool IsCurrentlyOffline()
        {
            return false;
        }

        public override bool IsDebugMode()
        {
            return true;
        }

        public override string GetSaveData()
        {
            return "SaveData";
        }
    }

}
