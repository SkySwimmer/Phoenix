using Newtonsoft.Json;
using Phoenix.Common;
using Phoenix.Common.IO;
using Phoenix.Common.Networking.Connections;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Phoenix.Client.ServerList
{
    /// <summary>
    /// Event handler for server detection
    /// </summary>
    /// <param name="instance">Server list entry</param>
    public delegate void DetectServerHandler(ServerInstance instance);

    /// <summary>
    /// Server list scanner utility
    /// </summary>
    public class ServerListScanner
    {
        /// <summary>
        /// Defines the API server used to scan the server list
        /// </summary>
        public static string API = "https://aerialworks.ddns.net/";

        private string _api = API;

        /// <summary>
        /// Defines if the scanner should filter servers with incompatible phoenix protocol versions
        /// </summary>
        public bool FilterIncompatiblePhoenixProtocolVersions = true;

        /// <summary>
        /// Defines if the scanner should filter servers with incompatible program protocol versions
        /// </summary>
        public bool FilterIncompatibleProgramProtocolVersions = true;

        /// <summary>
        /// Defines the Game ID used to scan the server list, null makes this use the current Game ID
        /// </summary>
        public string? GameID = null;

        /// <summary>
        /// Defines the program protocol version used to scan the server list
        /// </summary>
        public int ProgramProtocolVersion = -1;

        /// <summary>
        /// Called when a server is detected from the server list
        /// </summary>
        public event DetectServerHandler? OnDetectServer;

        /// <summary>
        /// Creates a server list scanner instance
        /// </summary>
        /// <param name="programProtocolVersion">Program protocol version</param>
        /// <param name="gameID">Game ID to use (null makes this use the current game ID)</param>
        /// <param name="apiServer">Defines the API server used to scan the server list</param>
        /// <param name="filterIncompatiblePhoenixVersions">True to filter incompatible phoenix versions, false otherwise</param>
        /// <param name="filterIncompatibleProgramVersions">True to filter incompatible program versions, false otherwise</param>
        public ServerListScanner(int programProtocolVersion, string? gameID = null, string? apiServer = null, bool filterIncompatiblePhoenixVersions = true, bool filterIncompatibleProgramVersions = true)
        {
            ProgramProtocolVersion = programProtocolVersion;
            GameID = gameID;
            if (apiServer != null)
                _api = apiServer;
            FilterIncompatiblePhoenixProtocolVersions = filterIncompatiblePhoenixVersions;
            FilterIncompatibleProgramProtocolVersions = filterIncompatiblePhoenixVersions;
        }

        /// <summary>
        /// Scans the public server list for servers
        /// </summary>
        /// <param name="timeout">Maximum amount of miliseconds to wait for servers to get listed</param>
        /// <param name="filters">Server list filters</param>
        /// <returns>Servers detected on the public server list</returns>
        public ServerInstance[] ScanPublicServerList(int timeout = 5000, params ServerListFilter[] filters)
        {
            // Build filters
            Dictionary<string, string> filterMap = new Dictionary<string, string>();
            foreach (ServerListFilter filter in filters)
            {
                string k = filter.Key;
                switch (filter.FilterType)
                {
                    case ServerListFilterType.STRICT:
                        k = "==" + k;
                        break;
                    case ServerListFilterType.LOOSE:
                        k = "=~" + k;
                        break;
                    case ServerListFilterType.REVERSE_STRICT:
                        k = "!=" + k;
                        break;
                    case ServerListFilterType.REVERSE_LOOSE:
                        k = "!~" + k;
                        break;
                }
                filterMap[k] = filter.FilterString;
            }

            // Find servers
            string gameID = GameID == null ? Game.GameID : GameID;
            List<ServerInstance> servers = new List<ServerInstance>();
            bool ended = false;
            try
            {
                // Contact Phoenix

                // Build URL
                string url = _api;
                if (!url.EndsWith("/"))
                    url += "/";
                url += "api/servers/serverlist/" + gameID;

                // Send request
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.Method = "POST";
                req.GetRequestStream().Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(filterMap)));
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
                    throw new IOException();
                }
                else
                {
                    MemoryStream buffer = new MemoryStream();

                    // Gather all servers
                    long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    while (true)
                    {
                        try
                        {
                            if (time + timeout < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                                break;
                            int b = resp.GetResponseStream().ReadByte();
                            if (b == -1)
                                break;
                            if (b == '\n')
                            {
                                // Read server entry
                                string entry = Encoding.UTF8.GetString(buffer.ToArray()).Replace("\r", "");
                                JsonServerListEntry? data = JsonConvert.DeserializeObject<JsonServerListEntry>(entry);
                                buffer = new MemoryStream();

                                // Verify
                                if (data != null && data.id != null && data.ownerId != null && data.protocol != null && data.version != null)
                                {
                                    // Ping on another thread
                                    Task.Run(() =>
                                    {
                                        // Call event
                                        ServerInstance inst = new ServerInstance(gameID, true, data.id, data.version, data.protocol.phoenixVersion, data.protocol.version, data.addresses, data.port, data.details);
                                        if (inst.IsReachable && !ended)
                                        {
                                            servers.Add(inst);
                                            OnDetectServer?.Invoke(inst);
                                        }
                                    });
                                }
                            }
                            else
                            {
                                buffer.WriteByte((byte)b);
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                    resp.GetResponseStream().Close();
                    resp.Close();

                    // Wait
                    if ((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - time) < timeout)
                        Thread.Sleep(timeout - (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - time));
                }
            }
            catch
            {
            }

            // Return servers
            ended = true;
            return servers.ToArray();
        }

        /// <summary>
        /// Scans the LAN network for servers
        /// </summary>
        /// <param name="timeout">Maximum amount of miliseconds to wait for servers to get listed</param>
        /// <returns>Servers detected in the lan</returns>
        public ServerInstance[] ScanLanServerList(int timeout = 5000)
        {
            string gameID = GameID == null ? Game.GameID : GameID;
            UdpClient client = new UdpClient();
            List<ServerInstance> servers = new List<ServerInstance>();
            List<string> entries = new List<string>();
            bool ended = false;
            try
            {
                // Connect
                client = new UdpClient();
                client.JoinMulticastGroup(IPAddress.Parse("224.0.2.232"), IPAddress.Any);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, 16719));
                client.Client.ReceiveTimeout = 5000;

                // Set magic
                byte[] magic = Encoding.UTF8.GetBytes("PHOENIX/LANDISCOVERY");

                // IP
                IPEndPoint op = new IPEndPoint(IPAddress.Any, 16719);

                // Send discovery command
                client.Send(magic, magic.Length, new IPEndPoint(IPAddress.Parse("224.0.2.232"), 16719));

                // Gather all servers
                long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                while (true)
                {
                    try
                    {
                        if (time + timeout < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                            break;
                        byte[] buffer = client.Receive(ref op);

                        // Wrap stream around it
                        MemoryStream strm = new MemoryStream(buffer);

                        // Handshake
                        bool pass = true;
                        for (int i = 0; i < magic.Length; i++)
                        {
                            if (strm.ReadByte() != magic[i])
                            {
                                pass = false;
                                break;
                            }
                        }
                        if (!pass || strm.Position == strm.Length)
                            continue;

                        // Read data
                        DataReader rd = new DataReader(strm);
                        string command = rd.ReadString();
                        if (command == "serverinfo")
                        {
                            // Secure mode
                            bool secureMode = rd.ReadBoolean();

                            // Port
                            int port = rd.ReadInt();

                            // Server id
                            string serverID = rd.ReadString();

                            // Protocol and version
                            string gameid = rd.ReadString();
                            string title = rd.ReadString();
                            string version = rd.ReadString();
                            int protocol = rd.ReadInt();
                            int phoenixProtocol = rd.ReadInt();

                            // Details
                            Dictionary<string, string> details = new Dictionary<string, string>();
                            int l = rd.ReadInt();
                            for (int i = 0; i < l; i++)
                            {
                                details[rd.ReadString()] = rd.ReadString();
                            }

                            string addr = op.Address.ToString();
                            if (addr.Contains("%"))
                                addr = addr.Remove(addr.IndexOf("%"));

                            // Found a server
                            if (!entries.Contains("[" + addr + "]:" + port) && gameID == gameid
                                && (!FilterIncompatiblePhoenixProtocolVersions || phoenixProtocol == Connections.PhoenixProtocolVersion)
                                && (!FilterIncompatibleProgramProtocolVersions || protocol == ProgramProtocolVersion))
                            {
                                entries.Add("[" + addr + "]:" + port);

                                // Ping on another thread
                                Task.Run(() =>
                                {
                                    // Call event
                                    ServerInstance inst = new ServerInstance(gameid, secureMode, secureMode ? serverID : null, version, phoenixProtocol, protocol, addr, true, port, details);
                                    if (inst.IsReachable && !ended)
                                    {
                                        servers.Add(inst);
                                        OnDetectServer?.Invoke(inst);
                                    }
                                });
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            try
            {
                client.Close();
            }
            catch { }
            ended = true;
            return servers.ToArray();
        }
    }
}
