using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Internal;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.Networking.Registry;
using System.Diagnostics;

namespace Phoenix.Common.Networking.Impl
{
    public class IntegratedClientBundle
    {
        private IntegratedClientConnection clientSide;
        private IntegratedClientConnection serverSide;

        public IntegratedClientBundle(IntegratedClientConnection clientSide, IntegratedClientConnection serverSide)
        {
            this.clientSide = clientSide;
            this.serverSide = serverSide;
        }

        public IntegratedClientConnection ClientSide
        {
            get
            {
                return clientSide;
            }
        }

        public IntegratedClientConnection ServerSide
        {
            get
            {
                return serverSide;
            }
        }
    }
    public class IntegratedClientConnection : Connection
    {
        private bool connected;
        private IntegratedClientConnection dest;
        private IntegratedServerConnection? server;
        private ConnectionSide side = ConnectionSide.CLIENT;

        public override ConnectionSide Side => side;

        public IntegratedClientConnection Other
        {
            get
            {
                return dest;
            }
        }

        public static IntegratedClientBundle Create(ChannelRegistry clientRegistry, ChannelRegistry serverRegistry)
        {
            IntegratedClientConnection server = new IntegratedClientConnection();
            IntegratedClientConnection client = new IntegratedClientConnection();
            client.Init(server, ConnectionSide.CLIENT, clientRegistry);
            server.Init(client, ConnectionSide.SERVER, serverRegistry);
            return new IntegratedClientBundle(client, server);
        }

        internal void SetServer(IntegratedServerConnection srv)
        {
            server = srv;
        }

        public void Init(IntegratedClientConnection dest, ConnectionSide side, ChannelRegistry registry)
        {
            if (this.dest != null)
                throw new InvalidOperationException("Already initialized");

            // Assign fields
            this.side = side;
            this.dest = dest;

            // Register channels
            foreach (PacketChannel ch in registry.Channels)
            {
                RegisterChannel(ch);
            }
        }

        public override void Close(string reason, params string[] args)
        {
            if (dest != null)
            {
                // Call disconnect code
                
                // Prevent infinite recursion
                IntegratedClientConnection d = dest;
                dest = null;
                if (server != null)
                {
                    server.DisconnectClient(d, reason, args);
                    server = null;
                }
                
                // Disconnect
                d.Close();
                connected = false;
                CallDisconnected(reason, args);
            }
        }

        private static bool warned = false;
        protected override void SendPacket(int cId, int id, AbstractNetworkPacket packet, PacketChannel channel)
        {
            // Send the packet to the other connection
            if (packet.LengthPrefixed)
            {
                MemoryStream strm = new MemoryStream();
                packet.Write(new DataWriter(strm));
                byte[] packetData = strm.ToArray();
                strm.Close();

                if (packet.Synchronized)
                {
                    strm = new MemoryStream(packetData);
                    DataReader reader = new DataReader(strm);
                    if (Debugger.IsAttached)
                    {
                        if (!dest.HandlePacket(cId, id, reader))
                        {
                            // Unhandled packet
                            // Log if in debug
                            if (Game.DebugMode)
                                Logger.GetLogger("Client: " + dest.ToString()).Error("Unhandled packet: " + packet.GetType().Name + ": [" + string.Concat(packetData.Select(x => x.ToString("x2"))) + "], channel type name: " + channel.GetType().Name);
                        }
                    }
                    else
                    {
                        try
                        {
                            if (!dest.HandlePacket(cId, id, reader))
                            {
                                // Unhandled packet
                                // Log if in debug
                                if (Game.DebugMode)
                                    Logger.GetLogger("Client: " + dest.ToString()).Error("Unhandled packet: " + packet.GetType().Name + ": [" + string.Concat(packetData.Select(x => x.ToString("x2"))) + "], channel type name: " + channel.GetType().Name);
                            }
                        }
                        catch
                        {
                        }
                    }
                    strm.Close();
                }
                else
                {
                    Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
                    {
                        MemoryStream strm = new MemoryStream(packetData);
                        DataReader reader = new DataReader(strm);
                        if (Debugger.IsAttached)
                        {
                            if (!dest.HandlePacket(cId, id, reader))
                            {
                                // Unhandled packet
                                // Log if in debug
                                if (Game.DebugMode)
                                    Logger.GetLogger("Client: " + dest.ToString()).Error("Unhandled packet: " + packet.GetType().Name + ": [" + string.Concat(packetData.Select(x => x.ToString("x2"))) + "], channel type name: " + channel.GetType().Name);
                            }
                        }
                        else
                        {
                            try
                            {
                                if (!dest.HandlePacket(cId, id, reader))
                                {
                                    // Unhandled packet
                                    // Log if in debug
                                    if (Game.DebugMode)
                                        Logger.GetLogger("Client: " + dest.ToString()).Error("Unhandled packet: " + packet.GetType().Name + ": [" + string.Concat(packetData.Select(x => x.ToString("x2"))) + "], channel type name: " + channel.GetType().Name);
                                }
                            }
                            catch
                            {
                            }
                        }
                        strm.Close();
                    });
                }
            }
            else
            {
                if (!warned)
                {
                    // Warn
                    Logging.Logger.GetLogger("Integrated Connection").Warn("WARNING! Usage of unprefixed packets over Integrated Connections is intensive!");
                    Logging.Logger.GetLogger("Integrated Connection").Warn("");
                    Logging.Logger.GetLogger("Integrated Connection").Warn("It is not recommended to use this method for integrated connections as it involves a loopback stream to get large data from one end to the other.");
                    Logging.Logger.GetLogger("Integrated Connection").Warn("For singleplayer, please use a different method to get this data across.");
                    Logging.Logger.GetLogger("Integrated Connection").Warn("This message will only show once.");
                    Logging.Logger.GetLogger("Integrated Connection").Warn("");
                    Logging.Logger.GetLogger("Integrated Connection").Warn("This message cannot be disabled and WILL display in log even outside of debug mode.");
                    warned = true;
                }
                // This is a bit more tricky, unprefixed packets need to be sent and received in raw at the same time
                // We use a special stream for this that blocks until read from the same one, both ways
                LoopbackStream strm = new LoopbackStream();
                Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() => {
                    packet.Write(new DataWriter(strm));
                });
                DataReader reader = new DataReader(strm);
                if (!dest.HandlePacket(cId, id, reader))
                {
                    // Unhandled packet
                    // Log if in debug
                    if (Game.DebugMode)
                        Logger.GetLogger("Client: " + dest.ToString()).Error("Unhandled packet: " + packet.GetType().Name + ", channel type name: " + channel.GetType().Name);
                }
                strm.Close();
            }
        }

        public override void Open()
        {
            // Dont allow this twice
            if (connected)
                throw new InvalidOperationException("Already connected");

            // Only on client side, to make it at least a bit realistic
            if (side != ConnectionSide.CLIENT)
                throw new InvalidOperationException("Cannot connect from a server-side connection");

            // Switch connected to true
            connected = true;
            dest.connected = true;

            // Run the connected event on both connections
            // Yeah its tricky as we want handshakes to remain functional
            LoopbackStream strm1 = new LoopbackStream();
            LoopbackStream strm2 = new LoopbackStream();
            if (server != null)
                Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
                {
                    server.DoCallConnected(Other, new ConnectionEventArgs(new DataWriter(strm1), new DataReader(strm2)));
                });
            Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
            {
                dest.CallConnected(new ConnectionEventArgs(new DataWriter(strm1), new DataReader(strm2)));
            });
            CallConnected(new ConnectionEventArgs(new DataWriter(strm2), new DataReader(strm1)));
            strm1.Close();
            strm2.Close();
            if (server != null)
                server.DoCallConnectionSuccess(Other);
            dest.CallConnectionSuccess();
            CallConnectionSuccess();
        }

        public override bool IsConnected()
        {
            return connected;
        }

        public override string GetRemoteAddress()
        {
            return "INTEGRATED-" + (side == ConnectionSide.CLIENT ? "SERVER" : "CLIENT");
        }

        public override string ToString()
        {
            return "Client [REMOTE: INTEGRATED-" + (side == ConnectionSide.CLIENT ? "SERVER]" : "CLIENT]");
        }
    }
}
