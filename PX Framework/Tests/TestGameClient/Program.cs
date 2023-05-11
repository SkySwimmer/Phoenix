using Newtonsoft.Json;
using Phoenix.Client;
using Phoenix.Client.Authenticators;
using Phoenix.Client.Authenticators.PhoenixAPI;
using Phoenix.Client.Components;
using Phoenix.Client.Factory;
using Phoenix.Client.IntegratedServerBootstrapper;
using Phoenix.Client.Providers;
using Phoenix.Client.ServerList;
using Phoenix.Common;
using Phoenix.Common.Certificates;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Impl;
using Phoenix.Common.Networking.Registry;
using Phoenix.Common.SceneReplication;
using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Debug.DebugServerRunner;
using Phoenix.Server;
using Phoenix.Tests.Server;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestGameClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Test client
            Logger.GlobalLogLevel = LogLevel.DEBUG; // Debug logging is just too much
            Logger.GetLogger("test").Info("Starting test client...");
            new GameImpl().Register();
            AssetManager.AddProvider(new FileAssetProvider());

            // Test server list pinger
            /*ServerListScanner scanner = new ServerListScanner(1, "soty");
            scanner.OnDetectServer += server =>
            {
                server = server;
            };
            scanner.ScanPublicServerList(filters: new ServerListFilter("ownerId", "00000000-0000-0000-0000-000000000000"));
            string best = new ServerInstance("soty", true, null, "1.0.0", 2, 1, new string[] { "aerialworks.ddns.net", "127.0.0.1", "192.168.1.65" }, 12345, new Dictionary<string, string>()).BestAddress;
            Console.Write("Login token: ");
            LoginManager.LoginToken = Console.ReadLine();
            LoginManager.Login(new Dictionary<string, object>(), t => { }, t => { }, t => { });*/

            ChannelRegistry registry = new ChannelRegistry();
            registry.Register(new SceneReplicationChannel());
            registry.Register(new TestChannel());

            int i = 1;
            Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
            {
                while (true)
                {
                    GameClient.GlobalTick();
                }
            });
            while (true)
            {
                for (int i2 = 0; i2 < 20; i2++)
                {
                    GameClientFactory fac = new GameClientFactory();
                    fac.WithAuthenticator(new PhoenixAuthenticator(new PhoenixSession("test-" + i, "test-" + i)));
                    fac.WithComponent(new TaskManagerComponent());
                    fac.WithComponent(new SceneReplicationComponent());
                    fac.WithChannelRegistry(registry);
                    fac.WithProtocolVersion(1);
                    fac.WithNetworkClient("127.0.0.1", 12345);
                    fac.WithAllowInsecureMode(true, () =>
                    {
                        // Some method that is called in case the server is in insecure mode

                        return true; // Allow this, false cancels the connection
                    });
                    fac.WithAutoInit(true);
                    fac.WithAutoConnect(true);

                    // Build client
                    GameClientBuildResult res = fac.Build("client-" + i);
                    if (!res.IsSuccess)
                    {
                        Console.Error.WriteLine("Error: failed to set up the client");
                        if (res.DisconnectReason != null)
                            Console.Error.WriteLine("Disconnect message: " + res.DisconnectReason.Reason);
                        Console.ReadLine();
                        return;
                    }
                    else
                        StartClient(res.Client, "test-" + i);
                    i++;
                }
                Console.WriteLine("Press enter to add 20 more clients");
                Console.ReadLine();
            }
            /*GameClient client = res.Client;
            while (client.IsConnected())
            {
                string message = Console.ReadLine();
                if (message == "/exit")
                {
                    client.Disconnect();
                    return;
                }
                if (message == "/tps")
                {
                    Console.WriteLine("Client TPS: " + client.TPS);
                    continue;
                }
                client.ClientConnection.GetChannel<TestChannel>().SendPacket(new TestPacket()
                {
                    Sender = "",
                    Message = message
                });
            }*/

            /*
            string server = File.ReadAllText("server.id.info");
            string phoenix = File.ReadAllText("px.info");
            Connection conn = Connections.CreateNetworkClient("localhost", 16719, registry, PXClientsideCertificate.Download(phoenix, File.ReadAllText("game.id.info"), server));
            Console.Write("Token: ");
            string token = Console.ReadLine();

            // Contact Phoenix to log into the server
            HttpClient cl = new HttpClient();
            string payload = JsonConvert.SerializeObject(new Dictionary<string, object>() { ["serverID"] = server });
            cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string result = cl.PostAsync(phoenix + "/api/auth/joinserver", new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
            AuthResponse? response = JsonConvert.DeserializeObject<AuthResponse>(result);
            if (response == null || response.secret == null)
                throw new IOException();

            conn.CustomHandshakes += (conn, args) => {
                // Phoenix handshake
                string gid = args.ClientInput.ReadString();
                int prot = args.ClientInput.ReadInt();
                args.ClientOutput.WriteString(gid);
                args.ClientOutput.WriteInt(prot);

                // The details used to connect
                args.ClientOutput.WriteString("localhost");
                args.ClientOutput.WriteInt(16719);
            };
            conn.Connected += (conn, args) => {
                args.ClientOutput.WriteString(response.secret);
                try
                {
                    args.ClientInput.ReadBoolean();
                }
                catch
                {
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            };
            conn.Open();
            while (true)
            {
                string message = Console.ReadLine();
                conn.GetChannel<TestChannel>().SendPacket(new TestPacket()
                {
                    Sender = "",
                    Message = message
                });
            }
            */
        }

        private static Random rnd = new Random();
        private static void StartClient(GameClient client, string id)
        {
            // Send a message
            client.ClientConnection.GetChannel<TestChannel>().SendPacket(new TestPacket()
            {
                Sender = "",
                Message = "Hello"
            });

            // Sync initial
            TestSyncPacket sync = new TestSyncPacket();
            sync.Transform = new Phoenix.Common.SceneReplication.Packets.Transform();
            sync.Transform.Position.X = rnd.Next(-100, 100);
            sync.Transform.Position.Z = rnd.Next(-100, 100);
            sync.Transform.Position.Y = 476.3808f;
            sync.Transform.Scale = new Phoenix.Common.SceneReplication.Packets.Vector3(1, 1, 1);
            client.ClientConnection.GetChannel<TestChannel>().SendPacket(sync);

            // Sync script
            int r = 0;
            new Thread(() =>
            {
                while (true)
                {
                    // Spinnnn
                    r += 30;
                    if (r >= 360)
                        r = 0;
                    sync.Transform.Rotation.Y = r;
                    client.ClientConnection.GetChannel<TestChannel>().SendPacket(sync);
                    Thread.Sleep(500);
                }
            })
            {
                IsBackground = true
            }.Start();
        }
    }

    class ConnectionComponent : ClientComponent, IClientConnectionProvider
    {
        public override string ID => "network-client";

        public Connection Provide()
        {
            return Connections.CreateNetworkClient("localhost", 16719, Client.ChannelRegistry, PXClientsideCertificate.Download("https://aerialworks.ddns.net", Game.GameID, Connections.DownloadServerID("localhost", 16719)));
        }

        public IClientConnectionProvider.ConnectionInfo ProvideInfo()
        {
            return new IClientConnectionProvider.ConnectionInfo("localhost", 16719);
        }

        public void StartGameClient()
        {
            Client.ClientConnection.Open();
        }

        public void StopGameClient()
        {
            Client.ClientConnection.Close();
        }

        protected override void Define()
        {
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
            return "../../../../Tests/Phoenix.Tests.Server/Assets";
        }

        public override string GetDevelopmentStage()
        {
            return "Alpha";
        }

        public override string GetGameFiles()
        {
            return "../../../../Tests/Phoenix.Tests.Server/Assets";
        }

        public override string GetGameID()
        {
            return "soty";
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
