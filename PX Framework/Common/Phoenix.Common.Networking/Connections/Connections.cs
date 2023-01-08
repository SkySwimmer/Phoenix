using Phoenix.Common.Certificates;
using Phoenix.Common.IO;
using Phoenix.Common.Networking.Impl;
using Phoenix.Common.Networking.Registry;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Phoenix.Common.Networking.Connections
{
    /// <summary>
    /// Used to create connections
    /// </summary>
    public static class Connections
    {
        /// <summary>
        /// The basic low-level protocol version of Phoenix itself
        /// </summary>
        public const int PhoenixProtocolVersion = 2;

        /// <summary>
        /// Creates a integrated connection bundle
        /// </summary>
        /// <param name="channels">Channel registry</param>
        /// <returns>ConnectionBundle instance</returns>
        public static ConnectionBundle CreateIntegratedConnections(ChannelRegistry channels)
        {
            IntegratedClientConnection client = new IntegratedClientConnection();
            IntegratedClientConnection server = new IntegratedClientConnection();
            IntegratedServerConnection srv = new IntegratedServerConnection();
            client.Init(server, ConnectionSide.CLIENT, channels);
            server.Init(client, ConnectionSide.SERVER, channels);
            srv.AddClient(server);
            return new ConnectionBundle(client, srv);
        }

        /// <summary>
        /// Creates a network client connection
        /// </summary>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server port</param>
        /// <param name="channels">Channel registry</param>
        /// <param name="certificate">Phoenix certificate</param>
        /// <returns>Connection instance</returns>
        public static Connection CreateNetworkClient(string ip, int port, ChannelRegistry channels, PXClientsideCertificate? certificate)
        {
            NetworkClientConnection conn = new NetworkClientConnection();
            conn.InitClient(ip, port, channels, certificate, ip);
            return conn;
        }

        public class ServerInfo
        {
            public string Address;
            public int Port;
            public string GameID = "";
            public string ServerID = "";
            public bool SecureMode;
        }

        /// <summary>
        /// Retrieves the server and game ID of a server (for certificate downloading)
        /// </summary>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server port</param>
        /// <returns>ServerInfo object</returns>
        public static ServerInfo DownloadServerInfo(string ip, int port)
        {
            TcpClient client = new TcpClient(ip, port);

            // Send hello
            byte[] hello = Encoding.UTF8.GetBytes("PHOENIX/HELLO/" + PhoenixProtocolVersion);
            byte[] helloSrv = Encoding.UTF8.GetBytes("PHOENIX/HELLO/SERVER/" + PhoenixProtocolVersion);
            client.GetStream().Write(hello);
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

            // Set mode to info
            client.GetStream().WriteByte(0);

            // Read
            ServerInfo info = new ServerInfo();
            info.Address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            info.Port = ((IPEndPoint)client.Client.RemoteEndPoint).Port;
            DataReader rd = new DataReader(client.GetStream());
            info.GameID = rd.ReadString();
            info.ServerID = rd.ReadString();
            info.SecureMode = rd.ReadBoolean();
            client.Close();
            return info;
        }

        /// <summary>
        /// Retrieves the ID of a server (for certificate downloading)
        /// </summary>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server port</param>
        /// <returns>Server ID string</returns>
        public static string DownloadServerID(string ip, int port)
        {
            TcpClient client = new TcpClient(ip, port);

            // Send hello
            byte[] hello = Encoding.UTF8.GetBytes("PHOENIX/HELLO/" + PhoenixProtocolVersion);
            byte[] helloSrv = Encoding.UTF8.GetBytes("PHOENIX/HELLO/SERVER/" + PhoenixProtocolVersion);
            client.GetStream().Write(hello);
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

            // Set mode to info
            client.GetStream().WriteByte(0);

            // Read
            DataReader rd = new DataReader(client.GetStream());
            rd.ReadString();
            string serverID = rd.ReadString();
            rd.ReadBoolean();
            client.Close();
            return serverID;
        }

        /// <summary>
        /// Retrieves the game ID of a server
        /// </summary>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server port</param>
        /// <returns>Game ID string</returns>
        public static string DownloadServerGameID(string ip, int port)
        {
            TcpClient client = new TcpClient(ip, port);

            // Send hello
            byte[] hello = Encoding.UTF8.GetBytes("PHOENIX/HELLO/" + PhoenixProtocolVersion);
            byte[] helloSrv = Encoding.UTF8.GetBytes("PHOENIX/HELLO/SERVER/" + PhoenixProtocolVersion);
            client.GetStream().Write(hello);
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

            // Set mode to info
            client.GetStream().WriteByte(0);

            // Read
            DataReader rd = new DataReader(client.GetStream());
            string gameID = rd.ReadString();
            rd.ReadString();
            rd.ReadBoolean();
            client.Close();
            return gameID;
        }

        /// <summary>
        /// Creates a network server
        /// </summary>
        /// <param name="address">Server address</param>
        /// <param name="port">Server port</param>
        /// <param name="channels">Channel registry</param>
        /// <param name="certificate">Server certificate</param>
        /// <returns>New ServerConnection instance</returns>
        public static ServerConnection CreateNetworkServer(IPAddress address, int port, ChannelRegistry channels, PXServerCertificate? certificate)
        {
            TcpListener listener = new TcpListener(address, port);
            NetworkServerConnection conn = new NetworkServerConnection();
            conn.Init(listener, channels, certificate);
            return conn;
        }

        /// <summary>
        /// Creates a network server
        /// </summary>
        /// <param name="port">Server port</param>
        /// <param name="channels">Channel registry</param>
        /// <param name="certificate">Server certificate</param>
        /// <returns>New ServerConnection instance</returns>
        public static ServerConnection CreateNetworkServer(int port, ChannelRegistry channels, PXServerCertificate? certificate)
        {
            return CreateNetworkServer(IPAddress.Any, port, channels, certificate);
        }
    }
}