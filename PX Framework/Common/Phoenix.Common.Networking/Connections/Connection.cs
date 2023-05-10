using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Internal;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.Networking.Connections
{
    /// <summary>
    /// Custom handshake utility
    /// </summary>
    /// <param name="connection">Connection object</param>
    /// <param name="args">Event arguments</param>
    /// <returns>True if the handshake succeeded, false otherwise</returns>
    public delegate void CustomHandshakeProvider(Connection connection, HandshakeEventArgs args);

    /// <summary>
    /// Connection event handler
    /// </summary>
    /// <param name="connection">Connection object</param>
    /// <param name="args">Connection event args</param>
    public delegate void ConnectionEventHandler(Connection connection, ConnectionEventArgs args);

    /// <summary>
    /// Connection success event handler
    /// </summary>
    /// <param name="connection">Connection object</param>
    public delegate void ConnectionSuccessEventHandler(Connection connection);

    /// <summary>
    /// Disconnect event handler
    /// </summary>
    /// <param name="connection">Connection object</param>
    /// <param name="reason">Disconnect reason</param>
    /// <param name="args">Disconnect reason arguments</param>
    public delegate void ConnectionDisconnectEventHandler(Connection connection, string reason, string[] args);

    /// <summary>
    /// 'Connected' event object
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        private DataWriter _out;
        private DataReader _in;
        private bool _failed;

        public ConnectionEventArgs(DataWriter @out, DataReader @in)
        {
            _out = @out;
            _in = @in;
        }

        /// <summary>
        /// Client input reader
        /// </summary>
        public DataReader ClientInput
        {
            get
            {
                return _in;
            }
        }

        /// <summary>
        /// Client output writer
        /// </summary>
        public DataWriter ClientOutput
        {
            get
            {
                return _out;
            }
        }
    }

    /// <summary>
    /// Basic connection class (use the Connections helper type to create connections)
    /// </summary>
    public abstract class Connection
    {
        private List<object> objects = new List<object>();
        private Dictionary<PacketChannel, PacketHandler> channels = new Dictionary<PacketChannel, PacketHandler>();
        private Logger logger;

        /// <summary>
        /// Disconnect reason parameters
        /// </summary>
        public DisconnectParams? DisconnectReason { get; protected set; } = null;

        /// <summary>
        /// Event for custom handshakes
        /// </summary>
        public event CustomHandshakeProvider? CustomHandshakes;

        /// <summary>
        /// Attempts a custom handshake
        /// </summary>
        /// <param name="connection">Connection object</param>
        /// <param name="clientOutput">Client output stream</param>
        /// <param name="clientInput">Client input stream</param>
        /// <returns>True if the handshake succeeded, false otherwise</returns>
        protected bool AttemptCustomHandshake(Connection connection, DataWriter clientOutput, DataReader clientInput)
        {
            HandshakeEventArgs args = new HandshakeEventArgs(clientOutput, clientInput);
            CustomHandshakes?.Invoke(connection, args);
            return !args.HasFailed();
        }

        /// <summary>
        /// Retrieves connection objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <returns>Object instance or default (typically null)</returns>
        public T? GetObject<T>()
        {
            foreach (object obj in objects)
            {
                if (obj is T)
                    return (T)obj;
            }
            return default(T);
        }

        /// <summary>
        /// Adds connection objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object to add</param>
        public void AddObject<T>(T obj)
        {
            if (obj == null)
                return;
            T? old = GetObject<T>();
            if (old != null)
                objects.Remove(old);
            objects.Add(obj);
        }

        /// <summary>
        /// Event that is called when the connection is established
        /// </summary>
        public event ConnectionEventHandler? Connected;

        /// <summary>
        /// Event that is called when the packet handlers are started and the connection is safe to read from and write to
        /// </summary>
        public event ConnectionSuccessEventHandler? ConnectionSuccess;

        /// <summary>
        /// Event that is called on disconnect
        /// </summary>
        public event ConnectionDisconnectEventHandler? Disconnected;

        /// <summary>
        /// Retrieves the connection side
        /// </summary>
        public abstract ConnectionSide Side { get; }

        /// <summary>
        /// Sends a packet
        /// </summary>
        /// <param name="cId">Channel ID</param>
        /// <param name="id">Packet ID</param>
        /// <param name="packet">Packet to send</param>
        /// <param name="channel">Packet channel</param>
        protected abstract void SendPacket(int cId, int id, AbstractNetworkPacket packet, PacketChannel channel);

        /// <summary>
        /// Ends the connection
        /// </summary>
        /// <param name="reason">Disconnect reason message</param>
        /// <param name="args">Message parameters</param>
        public abstract void Close(string reason, params string[] args);

        /// <summary>
        /// Called to connect to the remote endpoint
        /// </summary>
        public abstract void Open();

        /// <summary>
        /// Checks if the connection is still open
        /// </summary>
        /// <returns>True if connected, false otherwise</returns>
        public abstract bool IsConnected();

        /// <summary>
        /// Retrieves the remote address
        /// </summary>
        /// <returns>Remote client address</returns>
        public abstract string GetRemoteAddress();

        /// <summary>
        /// Ends the connection
        /// </summary>
        /// <param name="reason">Disconnect reason message</param>
        public virtual void Close(string reason)
        {
            Close(reason, new string[0]);
        }

        /// <summary>
        /// Ends the connection
        /// </summary>
        public virtual void Close()
        {
            Close("disconnect.generic");
        }

        /// <summary>
        /// Retrieves a packet channel
        /// </summary>
        /// <typeparam name="T">Channel type</typeparam>
        /// <returns>PacketChannel instance</returns>
        public T GetChannel<T>() where T : PacketChannel
        {
            foreach (PacketChannel ch in channels.Keys)
            {
                if (ch is T)
                    return (T)ch;
            }
            throw new ArgumentException("No channel found");
        }

        /// <summary>
        /// Retrieves a packet channel
        /// </summary>
        /// <param name="cId">Channel ID</param>
        /// <returns>PacketChannel instance or null</returns>
        public PacketChannel? GetChannel(int cId)
        {
            if (cId >= 0 && cId < channels.Count)
                return channels.Keys.ToArray()[cId];
            return null;
        }

        /// <summary>
        /// Retrieves a packet channel
        /// </summary>
        /// <param name="type">Channel to find the instance of</param>
        /// <returns>PacketChannel instance</returns>
        public PacketChannel GetChannel(PacketChannel type)
        {
            foreach (PacketChannel ch in channels.Keys)
            {
                if (type.GetType().IsAssignableFrom(ch.GetType()))
                    return ch;
            }
            throw new ArgumentException("No channel found");
        }

        /// <summary>
        /// Registers a packet channel in this connection
        /// </summary>
        /// <param name="channel">Packet channel to register</param>
        protected void RegisterChannel(PacketChannel channel)
        {
            PacketChannel ch = channel.Instantiate();

            // Init
            PacketHandler pHandler = null;
            ch.InitializeChannel(channel, new ConnectionContextImplementer(() => {
                return this;
            }, (handler) => {
                pHandler = handler;
            }, (id, packet, channel) => {
                int i = 0;
                foreach (PacketChannel ch in channels.Keys)
                {
                    if (channel == ch)
                    {
                        SendPacket(i, id, packet, channel);
                        break;
                    }
                    i++;
                }
            }));

            // Add to the list
            channels[ch] = pHandler;
        }

        /// <summary>
        /// Handles a packet
        /// </summary>
        /// <param name="cID">Channel ID</param>
        /// <param name="pID">Packet ID</param>
        /// <param name="reader">Packet payload reader</param>
        /// <returns>True if handled, false otherwise</returns>
        protected bool HandlePacket(int cID, int pID, DataReader reader)
        {
            PacketChannel[] chs = channels.Keys.ToArray();
            if (cID < chs.Length)
            {
                PacketChannel channel = chs[cID];

                // Handle
                if (channels[channel](pID, reader))
                    return true;
                return false;
            }
            return false;
        }

        /// <summary>
        /// Calls the connection established event
        /// </summary>
        protected void CallConnected(ConnectionEventArgs args)
        {
            Connected?.Invoke(this, args);
        }

        /// <summary>
        /// Calls the connection success event
        /// </summary>
        protected void CallConnectionSuccess()
        {
            ConnectionSuccess?.Invoke(this);
        }

        /// <summary>
        /// Calls the connection disconnect event
        /// <param name="reason">Disconnect reason</param>
        /// <param name="args">Disconnect reason arguments</param>
        /// </summary>
        protected void CallDisconnected(string reason, string[] args)
        {
            Disconnected?.Invoke(this, reason, args);
            DisconnectReason = new DisconnectParams(reason, args);
        }

        /// <summary>
        /// Calls the connection established event
        /// </summary>
        protected void CallConnected(Connection conn, ConnectionEventArgs args)
        {
            Connected?.Invoke(conn, args);
        }

        /// <summary>
        /// Calls the connection success event
        /// </summary>
        protected void CallConnectionSuccess(Connection conn)
        {
            ConnectionSuccess?.Invoke(conn);
        }

        /// <summary>
        /// Calls the connection disconnect event
        /// <param name="reason">Disconnect reason</param>
        /// <param name="args">Disconnect reason arguments</param>
        /// </summary>
        protected void CallDisconnected(string reason, string[] args, Connection conn)
        {
            Disconnected?.Invoke(conn, reason, args);
            conn.DisconnectReason = new DisconnectParams(reason, args);
        }

    }
}
