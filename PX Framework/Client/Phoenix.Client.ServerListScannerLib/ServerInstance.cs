using Phoenix.Common.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Phoenix.Client.ServerList
{
    /// <summary>
    /// Phoenix Server Instance
    /// </summary>
    public class ServerInstance
    {
        private string _gameID;
        private bool _secureMode;
        private string? _serverID;
        private string _version;
        private int _phoenixProtocolVersion;
        private int _programProtocolVersion;
        private string[] _addresses;
        private int _port;
        private Dictionary<string, string> _details;

        private string? _bestAddress;
        private bool _unreachable;
        private int _ping = -1;

        private bool isLan;

        public ServerInstance(string gameID, bool secureMode, string? serverID, string version, int phoenixProtocolVersion, int programProtocolVersion, string[] addresses, int port, Dictionary<string, string> details)
        {
            _gameID = gameID;
            _secureMode = secureMode;
            _serverID = serverID;
            _version = version;
            _phoenixProtocolVersion = phoenixProtocolVersion;
            _programProtocolVersion = programProtocolVersion;
            _addresses = addresses;
            _port = port;
            _details = details;
        }

        public ServerInstance(string gameID, bool secureMode, string? serverID, string version, int phoenixProtocolVersion, int programProtocolVersion, string address, bool isLan, int port, Dictionary<string, string> details)
        {
            _gameID = gameID;
            _secureMode = secureMode;
            _serverID = serverID;
            _version = version;
            _phoenixProtocolVersion = phoenixProtocolVersion;
            _programProtocolVersion = programProtocolVersion;
            _addresses = new string[] { address };
            _bestAddress = address;
            _port = port;
            _details = details;
            this.isLan = isLan;
        }

        /// <summary>
        /// Checks if this is a lan server (checks mostly if it needs to connect with a different mode)
        /// </summary>
        public bool IsLanServer
        {
            get
            {
                return isLan;
            }
        }

        /// <summary>
        /// Retrieves the server game ID
        /// </summary>
        public string GameID
        {
            get
            {
                return _gameID;
            }
        }

        /// <summary>
        /// Checks if the server is in secure mode
        /// </summary>
        public bool SecureMode
        {
            get
            {
                return _secureMode;
            }
        }

        /// <summary>
        /// Retrieves the server ID (<b>does not work if the server is in insecure-mode</b>)
        /// </summary>
        public string ServerID
        {
            get
            {
                if (!_secureMode || _serverID == null)
                    throw new ArgumentException("Unable to retrieve server ID of insecure-mode servers");
                return _serverID;
            }
        }

        /// <summary>
        /// Retrieves the game version
        /// </summary>
        public string GameVersion
        {
            get
            {
                return _version;
            }
        }

        /// <summary>
        /// Retrieves the Phoenix protocol version
        /// </summary>
        public int PhoenixProtocolVersion
        {
            get
            {
                return _phoenixProtocolVersion;
            }
        }

        /// <summary>
        /// Retrieves the program-specific protocol version
        /// </summary>
        public int ProgramProtocolVersion
        {
            get
            {
                return _programProtocolVersion;
            }
        }

        /// <summary>
        /// Retrieves the server addresses
        /// </summary>
        public string[] Addresses
        {
            get
            {
                return _addresses;
            }
        }

        /// <summary>
        /// Retrieves the server port
        /// </summary>
        public int ServerPort
        {
            get
            {
                return _port;
            }
        }

        /// <summary>
        /// Retrieves the server details block
        /// </summary>
        public Dictionary<string, string> Details
        {
            get
            {
                return _details;
            }
        }

        private void PingServer()
        {
            // Find best address if needed
            if (_bestAddress == null)
            {
                // Find address
                Dictionary<string, int> pings = new Dictionary<string, int>();
                foreach (string address in Addresses)
                {
                    // Ping server
                    try
                    {
                        // Attempt connection
                        TcpClient client = new TcpClient();
                        IAsyncResult res = client.BeginConnect(address, _port, null, null);
                        bool success = res.AsyncWaitHandle.WaitOne(1000); // One second timeout
                        if (!success)
                            throw new IOException("Connect failed");
                        client.EndConnect(res);

                        // Attempt partial handshake so we can ping the server
                        long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        // Send hello
                        byte[] hello = Encoding.UTF8.GetBytes("PHOENIX/HELLO/" + PhoenixProtocolVersion);
                        byte[] helloSrv = Encoding.UTF8.GetBytes("PHOENIX/HELLO/SERVER/" + PhoenixProtocolVersion);
                        client.GetStream().Write(hello);

                        // Check response
                        int i2 = 0;
                        foreach (byte b in helloSrv)
                        {
                            int i = client.GetStream().ReadByte();
                            if (i == -1)
                            {
                                client.GetStream().WriteByte(0);
                                client.Close();
                                throw new IOException("Connection failed: connection lost during HELLO");
                            }
                            if (helloSrv[i2++] != i)
                            {
                                client.GetStream().WriteByte(0);
                                client.Close();
                                throw new IOException("Connection failed: invalid server response during HELLO");
                            }
                        }

                        // Get ping time
                        long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

                        // Set mode to info
                        client.GetStream().WriteByte(0);

                        // Lets do this cleanly
                        DataReader rd = new DataReader(client.GetStream());
                        rd.ReadString(); // Game ID
                        rd.ReadString(); // Server ID
                        rd.ReadBoolean(); // Secure-mode

                        // Disconnect
                        client.Close();
                        pings[address] = (int)time;
                    }
                    catch
                    {
                        // Unreachable address
                    }
                }

                // Find lowest ping
                int lowestPing = -1;
                string? bestAddr = null;
                foreach (string addr in pings.Keys)
                {
                    if (lowestPing == -1 || pings[addr] < lowestPing)
                    {
                        lowestPing = pings[addr];
                        bestAddr = addr;
                    }
                }
                if (bestAddr != null)
                {
                    _bestAddress = bestAddr;
                    _ping = lowestPing;
                }
                else
                {
                    _unreachable = true;
                }

                return;
            }

            // Ping server
            try
            {
                TcpClient client = new TcpClient(_bestAddress, _port);

                // Attempt partial handshake so we can ping the server
                long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Send hello
                byte[] hello = Encoding.UTF8.GetBytes("PHOENIX/HELLO/" + PhoenixProtocolVersion);
                byte[] helloSrv = Encoding.UTF8.GetBytes("PHOENIX/HELLO/SERVER/" + PhoenixProtocolVersion);
                client.GetStream().Write(hello);

                // Check response
                int i2 = 0;
                foreach (byte b in helloSrv)
                {
                    int i = client.GetStream().ReadByte();
                    if (i == -1)
                    {
                        client.GetStream().WriteByte(0);
                        client.Close();
                        throw new IOException("Connection failed: connection lost during HELLO");
                    }
                    if (helloSrv[i2++] != i)
                    {
                        client.GetStream().WriteByte(0);
                        client.Close();
                        throw new IOException("Connection failed: invalid server response during HELLO");
                    }
                }

                // Get ping time
                long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;

                // Set mode to info
                client.GetStream().WriteByte(0);

                // Lets do this cleanly
                DataReader rd = new DataReader(client.GetStream());
                rd.ReadString(); // Game ID
                rd.ReadString(); // Server ID
                rd.ReadBoolean(); // Secure-mode

                // Disconnect
                client.Close();
                _ping = (int)time;
            }
            catch
            {
                // Unreachable server
                _unreachable = true;
                _ping = -1;
            }
        }

        /// <summary>
        /// Checks if the server is reachable
        /// </summary>
        public bool IsReachable
        {
            get
            {
                if (_unreachable)
                    return false;
                PingServer();
                return !_unreachable;
            }
        }

        /// <summary>
        /// Retrieves the current server ping
        /// </summary>
        public int Ping
        {
            get
            {
                if (_unreachable)
                    return -1;
                PingServer();
                return _ping;
            }
        }

        /// <summary>
        /// Retrieves the best server IP address (<b>note: this will throw a exception if the server is unreachable</b>)
        /// </summary>
        public string BestAddress
        {
            get
            {
                if (_bestAddress == null)
                {
                    // Try ping
                    if (_unreachable)
                        throw new ArgumentException("Server unreachable");
                    PingServer();
                    if (_bestAddress == null)
                        throw new ArgumentException("Server unreachable");
                }
                return _bestAddress;
            }
        }
    }
}
