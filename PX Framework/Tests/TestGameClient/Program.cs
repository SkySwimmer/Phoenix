using Newtonsoft.Json;
using Phoenix.Client;
using Phoenix.Client.Authenticators;
using Phoenix.Client.Authenticators.PhoenixAPI;
using Phoenix.Client.Components;
using Phoenix.Client.Factory;
using Phoenix.Client.IntegratedServerBootstrapper;
using Phoenix.Client.Providers;
using Phoenix.Common;
using Phoenix.Common.Certificates;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Impl;
using Phoenix.Common.Networking.Registry;
using Phoenix.Common.SceneReplication;
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



            ChannelRegistry registry = new ChannelRegistry();
            registry.Register(new SceneReplicationChannel());
            registry.Register(new TestChannel());
            
            GameClientFactory fac = new GameClientFactory();
            Console.Write("Login token: ");
            LoginManager.LoginToken = Console.ReadLine();
            LoginManager.Login(new Dictionary<string, object>(), t => { }, t => { }, t => { });
            fac.WithAuthenticator(new PhoenixAuthenticator(LoginManager.Session));
            fac.WithComponent(new TaskManagerComponent());
            fac.WithComponent(new SceneReplicationComponent());
            fac.WithChannelRegistry(registry);
            fac.WithProtocolVersion(1);
            fac.WithNetworkClient("localhost", 16719);
            fac.WithAllowInsecureMode(true, () =>
            {
                // Some method that is called in case the server is in insecure mode

                return true; // Allow this, false cancels the connection
            });
            fac.WithAutoInit(true);
            fac.WithAutoConnect(true);

            // Build client
            GameClientBuildResult res = fac.Build("client-1");
            if (!res.IsSuccess)
                return;
            GameClient client = res.Client;
            Task.Run(() =>
            {
                while (true)
                {
                    GameClient.GlobalTick();
                }
            });
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
            }

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
