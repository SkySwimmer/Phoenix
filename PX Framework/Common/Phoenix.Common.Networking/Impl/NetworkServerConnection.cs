using Phoenix.Common.Certificates;
using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Impl;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.Networking.Registry;
using System.Net.Sockets;
using System.Text;

namespace Phoenix.Common.Networking.Connections
{
    public class NetworkServerConnection : ServerConnection
    {   
        private TcpListener listener;
        private PXServerCertificate? certificate;
        private ChannelRegistry registry;

        private List<Connection> clients = new List<Connection>();
        private bool opened = false;
        private Logger logger;

        public void UpdateCertificate(PXServerCertificate certificate)
        {
            if (this.certificate == null || (
                this.certificate.GameID == certificate.GameID &&
                this.certificate.ServerID == certificate.ServerID)) {
                Logger.GetLogger("Server: " + listener.LocalEndpoint).Trace("Updated server certificate!");
                this.certificate = certificate;
            }
            else
                throw new ArgumentException("Cannot swap certificate to a different server or game ID");
        }

        public override bool IsConnected()
        {
            return opened && listener != null;
        }

        public override void Open()
        {
            if (IsConnected())
                throw new InvalidOperationException("Server already running");
            if (opened)
                throw new InvalidOperationException("Server has been closed");
            logger = Logger.GetLogger("Server: " + listener.LocalEndpoint);

            // Start the listener
            logger.Trace("Starting server listener on " + listener.LocalEndpoint + "...");
            listener.Start();
            opened = true;

            // Client connection handler
            listener.BeginAcceptTcpClient(new AsyncCallback(AcceptedTcpClient), null);
            logger.Trace("Begun to accept clients...");
        }

        private void AcceptedTcpClient(IAsyncResult ar)
        {
            TcpClient client = listener.EndAcceptTcpClient(ar);
            logger.Trace("Client socket connected: " + client.Client.RemoteEndPoint);
            Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
            {
                // Initial handshake
                try
                {
                    // Send hello
                    logger.Trace("Attempting Phoenix networking handshake with protocol version " + Connections.PhoenixProtocolVersion + "...");
                    byte[] hello = Encoding.UTF8.GetBytes("PHOENIX/HELLO/" + Connections.PhoenixProtocolVersion);
                    byte[] helloSrv = Encoding.UTF8.GetBytes("PHOENIX/HELLO/SERVER/" + Connections.PhoenixProtocolVersion);
                    logger.Debug("Sending HELLO messsage...");
                    client.GetStream().Write(helloSrv);
                    int i2 = 0;
                    foreach (byte b in hello)
                    {
                        int i = client.GetStream().ReadByte();
                        if (i == -1)
                        {
                            logger.Trace("Received handshake HELLO packet is invalid");
                            client.GetStream().WriteByte(0);
                            client.Close();
                            throw new IOException("Connection failed: connection lost during HELLO");
                        }
                        if (hello[i2++] != i)
                        {
                            logger.Trace("Received handshake HELLO packet is invalid");
                            client.GetStream().WriteByte(0);
                            client.Close();
                            throw new IOException("Connection failed: invalid client response during HELLO");
                        }
                    }

                    // Read mode
                    logger.Debug("Reading client mode...");
                    int mode = client.GetStream().ReadByte();
                    logger.Debug("Client mode: " + mode);
                    if (mode != 0 && mode != 1)
                    {
                        logger.Debug("Invalid connection mode!");
                        client.Close();
                        throw new IOException("Connection failed: invalid mode");
                    }
                    if (mode == 0)
                    {
                        logger.Trace("Sending server information...");

                        // Mode: server info
                        DataWriter wr = new DataWriter(client.GetStream());
                        if (certificate == null)
                        {
                            logger.Trace("  game: unknown");
                            logger.Trace("  server id: unknown");
                            logger.Trace("  insecure");

                            wr.WriteString("unknown");
                            wr.WriteString("unknown");
                            wr.WriteBoolean(false);
                        }
                        else
                        {
                            logger.Trace("  game: " + certificate.GameID);
                            logger.Trace("  server id: " + certificate.ServerID);
                            logger.Trace("  secure");
                            wr.WriteString(certificate.GameID);
                            wr.WriteString(certificate.ServerID);
                            wr.WriteBoolean(true);
                        }

                        logger.Trace("Client disconnect: " + client.Client.RemoteEndPoint);
                        client.Close();
                        return;
                    }
                }
                catch
                {
                    return;
                }

                // Wrap the connection around it
                NetworkClientConnection conn = new NetworkClientConnection();
                conn.InitServer(client, registry, certificate, () =>
                {
                    return AttemptCustomHandshake(conn, conn.Writer, conn.Reader);
                });
                conn.Connected += (t, args) =>
                {
                    // Connection event

                    // Call connected
                    CallConnected(t, args);

                    // Add client if it is still connected
                    if (t.IsConnected())
                        lock(clients)
                        {
                            clients.Add(conn);
                        }
                };
                conn.ConnectionSuccess += (t) =>
                {
                    CallConnectionSuccess(t);
                };
                conn.Disconnected += (t, r, a) =>
                {
                    // Disconnect event

                    // Remove client
                    lock (clients)
                    {
                        if (clients.Contains(t))
                            clients.Remove(t);
                    }

                    // Call disconnected
                    CallDisconnected(r, a, t);
                };

                // Connect
                try
                {
                    conn.Open();
                }
                catch
                {
                }
            });
            listener.BeginAcceptTcpClient(new AsyncCallback(AcceptedTcpClient), null);
        }

        public override void Close()
        {
            Close("disconnect.server.closed");
        }

        public override void Close(string reason, params string[] args)
        {
            if (!IsConnected())
                throw new InvalidOperationException("Server not running");

            // Close all
            Connection[] clients = GetClients();
            foreach (Connection conn in clients)
            {
                if (conn != null && conn.IsConnected())
                    conn.Close(reason, args);
            }

            // Close listener
            try
            {
                listener.Stop();
            }
            catch { }
            listener = null;
        }

        public override Connection[] GetClients()
        {
            while (true)
            {
                try
                {
                    return clients.ToArray();
                }
                catch { }
            }
        }

        public void Init(TcpListener listener, ChannelRegistry registry, PXServerCertificate? certificate)
        {
            this.listener = listener;
            this.registry = registry;
            this.certificate = certificate;

            foreach (PacketChannel channel in registry.Channels)
            {
                RegisterChannel(channel);
            }
        }

    }
}
