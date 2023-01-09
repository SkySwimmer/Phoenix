using Newtonsoft.Json;
using Phoenix.Common;
using Phoenix.Common.IO;
using Phoenix.Common.Events;
using Phoenix.Common.Networking.Connections;
using Phoenix.Server.Configuration;
using Phoenix.Server.Events;
using Phoenix.Server.Players;
using System.Text;
using System.Net.Http;

namespace Phoenix.Server.Components
{
    public class AuthenticationManagerComponent : ServerComponent
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

        private bool IsSecureServer()
        {
            // Check server
            if (Server.ServerConnection is NetworkServerConnection)
            {
                AbstractConfigurationSegment conf = Server.GetConfiguration("server");
                if (!conf.HasEntry("phoenix-api-server"))
                    conf.SetString("phoenix-api-server", "https://aerialworks.ddns.net/api/servers");

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
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return false;
        }

        public override string ID => "authentication-manager";

        protected override string ConfigurationKey => "server";

        protected override void Define()
        {
            DependsOn("player-manager");
        }

        public class AuthResponse
        {
            public string accountID;
            public string displayName;
        }
       
        [EventListener]
        public void ConnectSuccess(ClientConnectSuccessEvent ev)
        {
            EventBus.Dispatch(new PlayerJoinEvent(Server, ev.Client.GetObject<Player>()));
        }

        [EventListener]
        public void ClientConnected(ClientConnectedEvent ev)
        {
            try
            {
                byte[] magic = Encoding.UTF8.GetBytes("PHOENIXAUTHSTART");
                ev.EventArgs.ClientOutput.WriteRawBytes(magic);
                for (int i = 0; i < magic.Length; i++)
                {
                    if (magic[i] != ev.EventArgs.ClientInput.ReadRawByte())
                    {
                        throw new Exception();
                    }
                }
            }
            catch
            {
                // Log debug warning
                GetLogger().Trace("WARNING! Failed to authenticate client due to the first bit of network traffic not being a Phoenix authentication packet.");
                GetLogger().Trace("Please make sure the order of loading for components subscribed to the ClientConnectedEvent event is the same on both client and server.");

                // Disconnect
                if (ev.Client.IsConnected())
                    ev.Client.Close();
                return;
            }

            try
            {
                PlayerManagerService manager = ServiceManager.GetService<PlayerManagerService>();
                if (!IsSecureServer())
                {
                    // Insecure-mode handshake
                    string playerID = ev.EventArgs.ClientInput.ReadString();
                    string displayName = ev.EventArgs.ClientInput.ReadString();
                    PlayerJoinResult res = manager.AddPlayer(ev.Client, playerID, displayName);
                    if (res.IsSuccess)
                    {
                        ev.KeepConnectionOpen();
                        ev.EventArgs.ClientOutput.WriteBoolean(true); // Connect success
                    }
                }
                else
                {
                    // Secure-mode handshake
                    string secret = ev.EventArgs.ClientInput.ReadString();

                    // Contact phoenix
                    AbstractConfigurationSegment conf = Server.GetConfiguration("server");
                    if (!conf.HasEntry("phoenix-api-server"))
                        conf.SetString("phoenix-api-server", "https://aerialworks.ddns.net/api/servers");
                    string url = conf.GetString("phoenix-api-server") + "/authenticateplayer";
                    AuthResponse response = null;
                    try
                    {
                        HttpClient cl = new HttpClient();
                        string payload = JsonConvert.SerializeObject(new Dictionary<string, object>() { ["secret"] = secret });
                        cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + conf.GetString("token"));
                        string result = cl.PostAsync(url, new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        response = JsonConvert.DeserializeObject<AuthResponse>(result);
                        if (response == null || response.accountID == null || response.displayName == null)
                            throw new IOException();
                    }
                    catch
                    {
                        GetLogger().Warn("Failed to authenticate client: " + ev.Client.GetRemoteAddress() + ": failed to retrieve player information from Phoenix.");
                        return;
                    }

                    // Add player
                    PlayerJoinResult res = manager.AddPlayer(ev.Client, response.accountID, response.displayName);
                    if (res.IsSuccess)
                    {
                        ev.KeepConnectionOpen();
                        ev.EventArgs.ClientOutput.WriteBoolean(true); // Connect success
                    }
                }
            }
            catch
            {
                if (ev.Client.IsConnected())
                    ev.Client.Close();
            }
        }
    }
}