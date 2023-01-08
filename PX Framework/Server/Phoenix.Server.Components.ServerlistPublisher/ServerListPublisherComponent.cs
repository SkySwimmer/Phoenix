using Newtonsoft.Json;
using Phoenix.Common;
using Phoenix.Common.IO;
using Phoenix.Common.Networking.Connections;
using Phoenix.Server.Components.ServerlistPublisher;
using Phoenix.Server.Configuration;
using Phoenix.Server.Events;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Phoenix.Server.Components
{
    /// <summary>
    /// Server list publishing component
    /// </summary>
    public class ServerListPublisherComponent : ServerComponent
    {
        private bool _overrideListEnabled = false;
        private bool _overrideLanDiscoveryEnabled = false;
        private bool _listEnabled = false;
        private bool _lanDiscoveryEnabled = false;
        private ServerListDetails _details = new ServerListDetails();
        public ServerListDetails Details
        {
            get
            {
                return _details;
            }
        }
        public bool ListEnabled
        {
            get
            {
                return _listEnabled;
            }
            set
            {
                _listEnabled = value;
                _overrideListEnabled = true;
            }
        }
        public bool LanDiscoveryEnabled
        {
            get
            {
                return _lanDiscoveryEnabled;
            }
            set
            {
                _lanDiscoveryEnabled = value;
                _overrideLanDiscoveryEnabled = true;
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
                                serverid = data["sub"].ToString();
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            serverid = "unset";
            return false;
        }

        public override string ID => "server-list-publisher";

        protected override string ConfigurationKey => "serverlist";

        protected override void Define()
        {
        }

        private string serverid = "unset";
        private bool connected = false;
        private bool warned = false;
        private bool started = false;
        private string lastError = null;
        private UdpClient client;
        public override void StartServer()
        {
            // Server list configuration
            if (!Configuration.HasEntry("publish-to-public-list") && !_overrideListEnabled)
                Configuration.SetBool("publish-to-public-list", true);
            if (!Configuration.HasEntry("enable-lan-discovery") && !_overrideLanDiscoveryEnabled)
                Configuration.SetBool("enable-lan-discovery", true);
            if (!Configuration.HasEntry("use-server-port"))
                Configuration.SetBool("use-server-port", true);
            if (!Configuration.GetBool("use-server-port") && !Configuration.HasEntry("server-port"))
                Configuration.SetInteger("server-port", Server.Port);
            if (!_overrideListEnabled)
                _listEnabled = Configuration.GetBool("publish-to-public-list");
            if (!_overrideLanDiscoveryEnabled)
                _lanDiscoveryEnabled = Configuration.GetBool("enable-lan-discovery");

            // Start the list
            bool secureMode = IsSecureServer();
            if (_listEnabled)
            {
                if (!secureMode)
                {
                    // Warn about insecure mode
                    GetLogger().Warn("Running in insecure/debug mode, unable to publish to the public server list.");
                    GetLogger().Warn("This server will NOT show up in the public server list.");
                }
                else
                {
                    AbstractConfigurationSegment conf = Server.GetConfiguration("server");
                    string phoenixAPI = conf.GetString("phoenix-api-server");
                    GetLogger().Info("Attempting to publish to the server list...");

                    // Refresh thread
                    Thread publisher = new Thread(() =>
                    {
                        while (Server.IsRunning())
                        {
                            try
                            {
                                // Build URL
                                string url = phoenixAPI;
                                if (!url.EndsWith("/"))
                                    url += "/";
                                url += "postservertolist";

                                // Build data
                                ServerPublishData data = new ServerPublishData();
                                data.port = Configuration.GetBool("use-server-port") ? Server.Port : Configuration.GetInteger("server-port");
                                data.version = Game.Version;
                                data.protocol = Server.ProtocolVersion;
                                data.phoenixProtocol = Connections.PhoenixProtocolVersion;

                                // Send request
                                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                                req.Method = "POST";
                                req.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
                                req.Headers.Add("X-Request-RNDID", Guid.NewGuid().ToString());
                                req.Headers.Add("Authorization", "Bearer " + Server.GetConfiguration("server").GetString("token"));
                                req.GetRequestStream().Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));
                                HttpWebResponse resp = null;
                                try
                                {
                                    resp = (HttpWebResponse)req.GetResponseAsync().GetAwaiter().GetResult(); 
                                }
                                catch (WebException ex)
                                {
                                    resp = (HttpWebResponse)ex.Response;
                                    if (resp == null)
                                        throw new IOException("API Unreachable");
                                }
                                if (resp.StatusCode != HttpStatusCode.OK)
                                {
                                    string err = (int)resp.StatusCode + ": " + resp.StatusDescription;
                                    byte[] rData = new byte[(int)resp.ContentLength];
                                    resp.GetResponseStream().Read(rData);
                                    string error = Encoding.UTF8.GetString(rData);
                                    resp.Close();

                                    if (!warned || lastError != error)
                                    {
                                        lastError = error;
                                        GetLogger().Warn("An error occurred publishing the server.");
                                        GetLogger().Warn("The Phoenix API responded with: " + err);
                                        GetLogger().Warn("Detailed message: " + error.Trim());
                                        warned = true;
                                    }
                                    connected = false;
                                    Thread.Sleep(5000);
                                    continue;
                                }
                                else
                                {
                                    warned = false;
                                    lastError = null;
                                    started = true;
                                    GetLogger().Info("Successfully started the server list publisher.");
                                    connected = true;
                                    MemoryStream buffer = new MemoryStream();
                                    while (Server.IsRunning())
                                    {
                                        int b = resp.GetResponseStream().ReadByte();
                                        if (b == -1)
                                            break;
                                        if (b == 0)
                                        {
                                            // Read packet
                                            string packet = Encoding.UTF8.GetString(buffer.ToArray());
                                            buffer = new MemoryStream();
                                            Dictionary<string, string>? msg = JsonConvert.DeserializeObject<Dictionary<string, string>>(packet);
                                            if (msg == null)
                                                break;

                                            // Handle packet
                                            string ev = msg["event"];
                                            switch (ev)
                                            {
                                                case "listensuccess":
                                                    {
                                                        if (msg["success"] != "true")
                                                        {
                                                            resp.GetResponseStream().Close();
                                                            resp.Close();
                                                            GetLogger().Warn("An error occurred publishing the server.");
                                                            GetLogger().Warn("Established a connection with the list however the response was unexpected.");
                                                            GetLogger().Warn("Error: " + msg["error"]);
                                                            warned = true;
                                                            throw new IOException("Listen error");
                                                        }
                                                        break;
                                                    }
                                                case "requestupdate":
                                                    {
                                                        SendServerDetails(true);
                                                        break;
                                                    }
                                                case "ping":
                                                    {
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        resp.GetResponseStream().Close();
                                                        resp.Close();
                                                        GetLogger().Warn("An error occurred publishing the server.");
                                                        GetLogger().Warn("Established a connection with the list however the response was unexpected.");
                                                        GetLogger().Warn("Unhandled packet: " + packet);
                                                        warned = true;
                                                        throw new IOException("Listen error");
                                                    }
                                            }
                                        }
                                        else
                                        {
                                            buffer.WriteByte((byte)b);
                                        }
                                    }
                                }
                                resp.GetResponseStream().Close();
                                resp.Close();
                            }
                            catch
                            {
                                connected = false;
                                if (!IsSecureServer())
                                {
                                    started = true;
                                    GetLogger().Error("ERROR! Server list publisher has crashed! Server dropped into insecure mode, please restart the server to resume listing.");
                                    break;
                                }
                                else
                                {
                                    if (!warned || lastError != null)
                                    {
                                        lastError = null;
                                        GetLogger().Warn("Failed to publish the server to the server list, please check the internet connection.");
                                        warned = true;
                                    }
                                }
                                Thread.Sleep(5000);
                            }
                        }
                    });
                    publisher.IsBackground = true;
                    publisher.Name = "Server List Publisher";
                    publisher.Start();
                    for (int i = 0; i < 500 && !started && !warned; i++)
                    {
                        Thread.Sleep(10);
                    }
                }
            }
            if (_lanDiscoveryEnabled)
            {
                // Lan discovery
                GetLogger().Info("Enabled lan discovery!");
                Thread th = new Thread(() => { 
                    while (Server.IsRunning())
                    {
                        try
                        {
                            client = new UdpClient();
                            client.JoinMulticastGroup(IPAddress.Parse("224.0.2.232"), IPAddress.Any);
                            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            client.Client.Bind(new IPEndPoint(IPAddress.Any, 16719));
                            client.Client.ReceiveTimeout = -1;
                         
                            while (true)
                            {
                                IPEndPoint op = new IPEndPoint(IPAddress.Any, 16719);
                                byte[] buffer = client.Receive(ref op);

                                // Parse data
                                byte[] magic = Encoding.UTF8.GetBytes("PHOENIX/LANDISCOVERY");
                                bool pass = true;
                                for (int i = 0; i < magic.Length; i++)
                                {
                                    if (buffer[i] != magic[i])
                                    {
                                        pass = false;
                                        break;
                                    }
                                }
                                if (!pass || buffer.Length != magic.Length)
                                    continue;

                                // Create status message
                                MemoryStream outp = new MemoryStream();
                                DataWriter writer = new DataWriter(outp);
                                outp.Write(magic);
                                writer.WriteString("serverinfo");

                                // Secure mode
                                writer.WriteBoolean(IsSecureServer());

                                // Add server port
                                writer.WriteInt(Configuration.GetBool("use-server-port") ? Server.Port : Configuration.GetInteger("server-port"));

                                // Write server ID
                                writer.WriteString(serverid);

                                // Write protocol and version
                                writer.WriteString(Game.GameID);
                                writer.WriteString(Game.Title);
                                writer.WriteString(Game.Version);
                                writer.WriteInt(Server.ProtocolVersion);
                                writer.WriteInt(Connections.PhoenixProtocolVersion);

                                // Write details
                                Dictionary<string, string> details = _details.Compile(false);
                                writer.WriteInt(details.Count);
                                foreach (string key in details.Keys)
                                {
                                    writer.WriteString(key);
                                    writer.WriteString(details[key]);
                                }

                                // Build message
                                byte[] msg = outp.ToArray();

                                // Send message
                                client.Send(msg, msg.Length, new IPEndPoint(IPAddress.Parse("224.0.2.232"), 16719));
                            }
                        }
                        catch
                        {
                        }
                        if (client != null)
                        {
                            try
                            {
                                client.DropMulticastGroup(IPAddress.Parse("224.0.2.232"));
                                client.Close();
                            }
                            catch
                            {
                            }
                            client = null;
                        }
                    }
                });
                th.Name = "Lan discovery thread";
                th.IsBackground = true;
                th.Start();
            }
            
            // Update system
            if (_lanDiscoveryEnabled || _listEnabled)
            {
                Thread th = new Thread(() =>
                {
                    while (Server.IsRunning())
                    {
                        EventBus.Dispatch(new ServerListUpdateEvent(Server, _details));
                        if (_details.HasChanged && _listEnabled)
                            SendServerDetails();
                        Thread.Sleep(1000);
                    }
                });
                th.Name = "Server List Updater";
                th.IsBackground = true;
                th.Start();
            }
        }

        public override void StopServer()
        {
            if (client != null)
            {
                try
                {
                    client.DropMulticastGroup(IPAddress.Parse("224.0.2.232"));
                    client.Close();
                }
                catch
                {
                }
                client = null;
            }
        }

        public void SendServerDetails(bool force = false)
        {
            bool secureMode = IsSecureServer();

            // Check secure mode and list settings
            if (_listEnabled && secureMode && ConnectedToServerList)
            {
                if (!force && !Details.HasChanged)
                    return;

                // Load url from config
                AbstractConfigurationSegment conf = Server.GetConfiguration("server");
                if (!conf.HasEntry("phoenix-api-server"))
                    conf.SetString("phoenix-api-server", "https://aerialworks.ddns.net/api/servers");
                string phoenixAPI = conf.GetString("phoenix-api-server");

                try
                {
                    // Build URL
                    string url = phoenixAPI;
                    if (!url.EndsWith("/"))
                        url += "/";
                    url += "updateserverstatus";

                    // Build data
                    Dictionary<string, string> data = Details.Compile();

                    // Send request
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Server.GetConfiguration("server").GetString("token"));
                    HttpResponseMessage resp = client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(data))).GetAwaiter().GetResult();
                    if (!resp.IsSuccessStatusCode)
                        throw new IOException("Refresh failure: " + (int)resp.StatusCode + ": " + resp.ReasonPhrase);
                }
                catch (Exception e)
                {
                    GetLogger().Debug("Server list update failure", e);
                }
            }
        }

        public bool ConnectedToServerList
        {
            get
            {
                return connected;
            }
        }

        private class ServerPublishData
        {
            public int port;
            public string version;
            public int protocol;
            public int phoenixProtocol;
        }
    }
}
