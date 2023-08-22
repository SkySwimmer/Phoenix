using Newtonsoft.Json;
using Phoenix.Common;
using Phoenix.Common.Networking.Connections;
using Phoenix.Server.Configuration;
using System.Text;
using Phoenix.Server.Players;

namespace Phoenix.Server.Components
{
    /// <summary>
    /// Player Manager Component
    /// </summary>
    public class PlayerManagerComponent : ServerComponent
    {
        public override string ID => "player-manager";
        protected override string ConfigurationKey => "playermanager";

        private string? _ownerPlayerID = null;

        /// <summary>
        /// Retrieves the player ID of the owner of the server
        /// </summary>
        public string? OwnerPlayerID
        {
            get
            {
                return _ownerPlayerID;
            }
        }

        protected override void Define()
        {
            OptDependsOn("server-list-publisher");
        }

        public override void PreInit()
        {
            // Register services
            ServiceManager.RegisterService(new PlayerManagerService(Server));
            ServiceManager.RegisterService(new PermissionManagerService(Server));
            ServiceManager.RegisterService(new PlayerDataService());
        }

        public override void Init()
        {
            // Support for other components
            if (Server.IsComponentLoaded("server-list-publisher"))
            {
                GetLogger().Info("Loading server list support...");
                PlayerManager.DependencyTools.LoadServerListPublisherSupport(this);
            }
        }

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

        public override void StartServer()
        {
            ServiceManager.GetService<PlayerManagerService>().Configure(Configuration);

            // Check server
            if (Server.ServerConnection is NetworkServerConnection)
            {
                AbstractConfigurationSegment conf = Server.GetConfiguration("server");
                if (!conf.HasEntry("phoenix-api-server"))
                    conf.SetString("phoenix-api-server", PhoenixEnvironment.DefaultAPIServer + "servers");

                // Verify certificate & token validity
                bool secureMode = conf.GetBool("secure-mode");
                if (secureMode)
                {
                    // Read server token
                    string? token = conf.GetString("token");
                    if (!secureMode)
                        token = "disabled";
                    else if (token == null || token == "undefined" || token.Split('.').Length != 3)
                    {
                        conf.SetString("token", "undefined");
                        secureMode = false;
                        token = "undefined";
                    }

                    // Check token validity
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
                            long tokenIssueTime = long.Parse(data["iat"].ToString());
                            if (data["cgi"].ToString() != Game.GameID)
                                throw new ArgumentException();

                            AbstractConfigurationSegment? certificate = conf.GetSegment("certificate");
                            if (certificate != null && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() <= (certificate.GetLong("expiry") - (7 * 24 * 60 * 60 * 1000)) && tokenIssueTime == certificate.GetLong("tokenIssueTime") && Enumerable.SequenceEqual(conf.GetStringArray("addresses"), certificate.GetStringArray("addressesInternal")))
                            {
                                // Valid
                                if (data.ContainsKey("owner"))
                                {
                                    _ownerPlayerID = data["owner"].ToString();
                                    if (ServiceManager.GetService<PermissionManagerService>().Enabled && ServiceManager.GetService<PermissionManagerService>().ShouldGrantOperatorPermissionsToOwner)
                                        GetLogger().Info("Granted server operator permissions to " + _ownerPlayerID);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
    }
}
