using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phoenix.Common;
using Phoenix.Common.Certificates;
using Phoenix.Common.Networking.Connections;
using Phoenix.Server.Configuration;
using Phoenix.Server.ServerImplementations;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Phoenix.Server.NetworkServerLib
{
    public class NetworkServerComponent : ServerComponent, IServerProvider
    {
        private static class Base64Url
        {
            public static string Encode(byte[] arg)
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("arg");
                }

                var s = Convert.ToBase64String(arg);
                return s
                    .Replace("=", "")
                    .Replace("/", "_")
                    .Replace("+", "-");
            }

            public static string ToBase64(string arg)
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("arg");
                }

                var s = arg
                        .PadRight(arg.Length + (4 - arg.Length % 4) % 4, '=')
                        .Replace("_", "/")
                        .Replace("-", "+");

                return s;
            }

            public static byte[] Decode(string arg)
            {
                return Convert.FromBase64String(ToBase64(arg));
            }
        }

        public override string ID => "network-server-provider";

        protected override string ConfigurationKey => "server";

        public ServerConnection ProvideServer()
        {
            // Check security configuration
            if (!Configuration.HasEntry("secure-mode"))
                Configuration.SetBool("secure-mode", true);
            if (!Configuration.HasEntry("addresses"))
                Configuration.SetStringArray("addresses", new string[0]);
            if (!Configuration.HasEntry("phoenix-api-server"))
                Configuration.SetString("phoenix-api-server", PhoenixEnvironment.DefaultAPIServer + "servers");
            bool secureMode = Configuration.GetBool("secure-mode");

            // Show warning if needed
            if (!secureMode)
            {
                GetLogger().Warn("WARNING! Running in INSECURE MODE!");
                GetLogger().Warn("While this disables encryption and potentially allows for offline play (depends on the game), this opens up the possibility for hackers to fake user credentials!");
                if (!Configuration.HasEntry("allow-offline-permissions"))
                    Configuration.SetBool("allow-offline-permissions", false);
                if (!Configuration.GetBool("allow-offline-permissions"))
                    GetLogger().Warn("To protect your server and to prevent permission takeover, the permission system will be disabled. (you can turn this protection off by setting allow-offline-permissions)");

                // Disable permission manager
                if (Server.IsComponentLoaded("player-manager"))
                    DisablePermissionManager();
            }

            // Read server token
            string? token = Configuration.GetString("token");
            if (!secureMode)
                token = "disabled";
            else if (token == null || token == "undefined" || token.Split('.').Length != 3)
            {
                Configuration.SetString("token", "undefined");
                secureMode = false;
                token = "undefined";
            }

            // Check token validity
            string serverID = "";
            long tokenIssueTime = -1;
            if (secureMode)
            {
                // Decode the payload
                string[] jwt = token.Split('.');
                string payloadEncoded = jwt[1];
                try
                {
                    string payload = Encoding.UTF8.GetString(Base64Url.Decode(payloadEncoded));
                    Dictionary<string, object>? data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
                    if (data == null || !data.ContainsKey("exp"))
                        throw new ArgumentException();

                    // Check expiry
                    long exp = long.Parse(data["exp"].ToString());
                    if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > exp)
                        throw new ArgumentException();
                    serverID = data["sub"].ToString();
                    tokenIssueTime = long.Parse(data["iat"].ToString());
                    if (data["cgi"].ToString() != Game.GameID)
                        throw new ArgumentException();
                }
                catch
                {
                    Configuration.SetString("token", "undefined");
                    secureMode = false;
                    token = "undefined";
                }
            }

            // Check certificate
            if (secureMode) { 
                AbstractConfigurationSegment? certificate = Configuration.GetSegment("certificate");
                if (certificate == null || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > (certificate.GetLong("expiry") - (7 * 24 * 60 * 60 * 1000)) || tokenIssueTime != certificate.GetLong("tokenIssueTime") || !Enumerable.SequenceEqual(Configuration.GetStringArray("addresses"), certificate.GetStringArray("addressesInternal")))
                {
                    // Attempt to refresh certificate
                    if (certificate == null)
                        certificate = Configuration.CreateSegment("certificate");

                    try
                    {
                        GetLogger().Info("Refreshing server certificate...");
                        HttpClient cl = new HttpClient();
                        string payload = JsonConvert.SerializeObject(new Dictionary<string, object>() { ["addresses"] = Configuration.GetStringArray("addresses", new string[0]) });
                        cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                        string result = cl.PostAsync(Configuration.GetString("phoenix-api-server") + "/refreshserver", new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        RefreshResponse? response = JsonConvert.DeserializeObject<RefreshResponse>(result);
                        if (response == null)
                            throw new IOException();

                        // Update token
                        token = response.token;
                        Configuration.SetString("token", token);
                        string[] jwt = token.Split('.');
                        string payloadEncoded = jwt[1];
                        try
                        {
                            payload = Encoding.UTF8.GetString(Base64Url.Decode(payloadEncoded));
                            Dictionary<string, object>? data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
                            if (data == null || !data.ContainsKey("exp"))
                                throw new ArgumentException();

                            // Wait until the token is valid
                            if (data.ContainsKey("nbf"))
                            {
                                long nbf = long.Parse(data["nbf"].ToString());
                                while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < nbf)
                                    Thread.Sleep(100);
                            }

                            // Check expiry
                            long exp = long.Parse(data["exp"].ToString());
                            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > exp)
                                throw new ArgumentException();
                            tokenIssueTime = long.Parse(data["iat"].ToString());
                        }
                        catch
                        {
                        }

                        // Set ID
                        serverID = response.identity;

                        // Update certificate
                        certificate.SetLong("expiry", response.certificate.expiry);
                        certificate.SetLong("lastUpdate", response.certificate.lastUpdate);
                        certificate.SetStringArray("addressesInternal", response.certificate.addresses);
                        Configuration.SetStringArray("addresses", response.certificate.addresses);
                        certificate.SetString("publicKey", response.certificate.publicKey);
                        certificate.SetString("privateKey", response.certificate.privateKey);
                        certificate.SetLong("tokenIssueTime", tokenIssueTime);
                    }
                    catch
                    {
                        GetLogger().Warn("Failed to refresh the server certificate!");
                        GetLogger().Warn("");
                        secureMode = false;
                        token = "undefined";
                    }
                }
            }

            // Check result
            if (token == "undefined")
            {
                // Warn
                GetLogger().Warn("WARNING! Running in INSECURE MODE!");
                GetLogger().Warn("Please attempt to obtain a server hosting token from the game's vendor and update the configuration.");
                GetLogger().Warn("");
                GetLogger().Warn("");
                GetLogger().Warn("The server will be started WITHOUT ENCRYPTION, player authentication is unavailable due to the server lacking a valid security token.");
                if (!Configuration.HasEntry("allow-offline-permissions"))
                    Configuration.SetBool("allow-offline-permissions", false);
                if (!Configuration.GetBool("allow-offline-permissions"))
                {
                    GetLogger().Warn("As a result, gameplay is not protected by Phoenix and can be tampered with. The permission system is disabled to protect your staff.");
                    GetLogger().Warn("Without security, players can connect and spoof their ID to pose as staff to the server software. Permissions are disabled to prevent actual sabotage.");
                    GetLogger().Warn("If you wish the permission system to remain enabled when in insecure mode, set allow-offline-permissions to true.");
                }
                GetLogger().Warn("");
                GetLogger().Warn("Holding startup for 30 seconds, please shut the server down if you wish to update the token.");
                Thread.Sleep(30000);

                // Disable permission manager
                if (Server.IsComponentLoaded("player-manager"))
                    DisablePermissionManager();
            }

            // Create connection
            PXServerCertificate srvCertificate = null;
            if (secureMode)
            {
                RefreshResponse resp = new RefreshResponse();
                resp.identity = serverID;
                resp.certificate = new CertificateObject();
                resp.certificate.addresses = Configuration.GetStringArray("certificate.addressesInternal");
                resp.certificate.expiry = Configuration.GetLong("certificate.expiry");
                resp.certificate.lastUpdate = Configuration.GetLong("certificate.lastUpdate");
                resp.certificate.privateKey = Configuration.GetString("certificate.privateKey");
                resp.certificate.publicKey = Configuration.GetString("certificate.publicKey");
                resp.token = token;
                srvCertificate = PXServerCertificate.FromJson(Game.GameID, JsonConvert.SerializeObject(resp));
            }
            return Connections.CreateNetworkServer(Server.Address == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(Server.Address), Server.Port, Server.ChannelRegistry, srvCertificate);
        }

        public class RefreshResponse
        {
            public string identity;
            public CertificateObject certificate;
            public string token;
        }

        public class CertificateObject
        {
            public long lastUpdate;
            public long expiry;
            public string[] addresses;
            public string publicKey;
            public string privateKey;
        }

        public override void PreInit()
        {
            if (Server.HasConfigManager)
            {
                GetLogger().Trace("Loading server config...");

                // Port
                GetLogger().Debug("Loading port from configuration...");
                int confPort = Server.GetConfiguration("server").GetInteger("port");
                if (confPort == -1)
                {
                    GetLogger().Debug("Adding port entry to server configuration... Setting port " + Server.Port + "...");
                    Server.GetConfiguration("server").SetInteger("port", Server.Port);
                    confPort = Server.GetConfiguration("server").GetInteger("port");
                    Server.Port = confPort;
                }
                else
                    Server.Port = confPort;
                GetLogger().Trace("Server port overridden from configuration: " + Server.Port);

                // IP
                GetLogger().Debug("Loadign IP from configuration...");
                string? confIP = Server.GetConfiguration("server").GetString("address");
                if (confIP == null)
                {
                    GetLogger().Debug("Adding address entry to server configuration... Setting address " + Server.Address + "...");
                    Server.GetConfiguration("server").SetString("address", Server.Address);
                    confIP = Server.GetConfiguration("server").GetString("address");
                    Server.Address = confIP;
                }
                else
                    Server.Address = confIP;
                GetLogger().Trace("Server IP overridden from configuration: " + Server.Address);
            }
        }

        public void StartGameServer()
        {
            Server.ServerLogger.Info("Starting server on port " + Server.Port + "...");
            Server.ServerConnection.Open();
        }

        public void StopGameServer()
        {
            Server.ServerConnection.Close();
        }

        protected override void Define()
        {
            OptDependsOn("player-manager");
        }

        private void DisablePermissionManager()
        {
            ServiceManager.GetService<Players.PermissionManagerService>().Enabled = false;
        }
    }
}
