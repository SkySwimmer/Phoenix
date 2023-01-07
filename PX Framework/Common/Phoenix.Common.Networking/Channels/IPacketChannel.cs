using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Internal;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.Networking.Channels
{
    /// <summary>
    /// Packet channel interface
    /// </summary>
    public abstract class PacketChannel
    {
        private ConnectionContext ctx;
        private List<InternalPacketHandler> Handlers = new List<InternalPacketHandler>();
        private List<AbstractNetworkPacket> Registry = new List<AbstractNetworkPacket>();

        private delegate bool SimpleHandler(AbstractNetworkPacket packet);
        private List<SimpleHandler> SingleTimeHandlers = new List<SimpleHandler>();

        private bool Locked = false;

        /// <summary>
        /// Registers a network packet
        /// </summary>
        /// <param name="packetType">Packet to register</param>
        protected void RegisterPacket(AbstractNetworkPacket packetType)
        {
            if (Locked)
                throw new InvalidOperationException("Registry has been locked");
            Logger.GetLogger("channel-registry").Trace("Registering packet: " + packetType.GetType().FullName);
            Registry.Add(packetType);
        }

        /// <summary>
        /// Registers a packet handler
        /// </summary>
        /// <typeparam name="T">Handler type</typeparam>
        /// <param name="handler">Packet handler to register</param>
        public void RegisterHandler<T>(PacketHandler<T> handler) where T : AbstractNetworkPacket
        {
            if (Locked)
                throw new InvalidOperationException("Registry has been locked");
            Logger.GetLogger("channel-registry").Trace("Registering packet handler: " + handler.GetType().FullName);
            Handlers.Add(handler);
        }

        internal void CallMakeRegistry()
        {
            MakeRegistry();
        }

        /// <summary>
        /// Initializes the channel (INTERNAL)
        /// </summary>
        internal void InitializeChannel(PacketChannel def, ConnectionContext ctx)
        {
            if (Locked)
                throw new InvalidOperationException("Already initialized");
            this.ctx = ctx;
            this.Handlers = new List<InternalPacketHandler>(def.Handlers);
            this.Registry = new List<AbstractNetworkPacket>(def.Registry);
            Locked = true;
            ctx.PassPacketHandler((id, reader) =>
            {
                // Find packet
                if (id < 0 || id > Registry.Count)
                    return false;
                AbstractNetworkPacket packet = Registry[id].Instantiate();
                byte[]? dataSnippet = null;
                packet.Parse(reader);

                bool handled = false;

                // Single-time handlers
                SimpleHandler[] handlers;
                while (true)
                {
                    try
                    {
                        handlers = SingleTimeHandlers.ToArray();
                        break;
                    }
                    catch
                    {
                    }
                }
                foreach (SimpleHandler handler in handlers)
                {
                    if (handler(packet))
                    {
                        handled = true;
                        SingleTimeHandlers.Remove(handler);
                        break;
                    }
                }

                // Find handler
                foreach (InternalPacketHandler handler in Handlers)
                {
                    if (handler.CanHandle(packet))
                    {
                        if (handler.Instantiate().Handle(packet, this))
                            return true;
                    }
                }
                return handled;
            });
        }

        /// <summary>
        /// Sends a packet
        /// </summary>
        /// <param name="packet">Packet to send</param>
        public void SendPacket(AbstractNetworkPacket packet)
        {
            if (!Locked)
                throw new InvalidOperationException("Not initialized");
            if (Registry.Any(t => t.GetType().IsAssignableFrom(packet.GetType())))
            {
                // Send it via the context wrapper
                ctx.SendPacket(Registry.FindIndex(t => t.GetType().IsAssignableFrom(packet.GetType())), packet, this);
            } 
            else
            {
                throw new ArgumentException("The given packet is not registered to this channel");
            }
        }

        /// <summary>
        /// Sends a packet and waits for a response
        /// </summary>
        /// <param name="packet">Packet to send</param>
        /// <param name="timeout">Wait timeout</param>
        /// <typeparam name="T">Response type</typeparam>
        public T? SendPacketAndWaitForResponse<T>(AbstractNetworkPacket packet, int timeout = 5000) where T : AbstractNetworkPacket
        {
            if (!Locked)
                throw new InvalidOperationException("Not initialized");
            if (Registry.Any(t => t.GetType().IsAssignableFrom(packet.GetType())))
            {
                // Send it via the context wrapper
                ctx.SendPacket(Registry.FindIndex(t => t.GetType().IsAssignableFrom(packet.GetType())), packet, this);
            }
            else
            {
                throw new ArgumentException("The given packet is not registered to this channel");
            }

            // Attach handler
            T? resp = default(T);
            bool received = false;
            SimpleHandler handler = pk => {
                if (pk is T)
                {
                    received = true;
                    resp = (T)pk;
                    return true;
                }
                return false;
            };
            SingleTimeHandlers.Add(handler);
            int i = 0;
            while (!received  && i++ < timeout)
                Thread.Sleep(1);
            if (SingleTimeHandlers.Contains(handler))
                SingleTimeHandlers.Remove(handler);

            return resp;
        }

        /// <summary>
        /// Retrieves packet definitions<br/>
        /// <br/>
        /// WARNING! This does NOT create a new instance of packets, it will return the registry entry. <b>This is not intended for creating packets, create packet instances directly or use packet.Instantiate() to create a instance.</b>
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <returns>AbstractNetworkPacket instance or null</returns>
        public AbstractNetworkPacket? GetPacketDefinition(int packetID)
        {
            if (packetID >= 0 && packetID < Registry.Count)
                return Registry[packetID];
            return null;
        }

        /// <summary>
        /// Retrieves packet hander definitions<br/>
        /// <br/>
        /// WARNING! This does NOT create a new instance of packet handlers, it will return the registry entry.
        /// </summary>
        /// <typeparam name="T">Packet handler type</typeparam>
        /// <returns>PacketHandler instance or null</returns>
        public T? GetHandlerDefinition<T>() where T : InternalPacketHandler
        {
            foreach (InternalPacketHandler handler in Handlers)
            {
                if (handler is T)
                    return (T)handler;
            }
            return null;
        }

        /// <summary>
        /// Retrieves which side the channel is running from
        /// </summary>
        public ConnectionSide Side
        {
            get
            {
                if (!Locked)
                    throw new InvalidOperationException("Not initialized");
                return Connection.Side;
            }
        }

        /// <summary>
        /// Retrieves the network connection this channel is attached to
        /// </summary>
        public Connection Connection
        {
            get
            {
                if (!Locked)
                    throw new InvalidOperationException("Not initialized");
                return ctx.GetConnection();
            }
        }

        /// <summary>
        /// Creates a new instance of the packet channel
        /// </summary>
        /// <returns>New PacketChannel instance</returns>
        public abstract PacketChannel Instantiate();

        /// <summary>
        /// Used to build the packet registry
        /// </summary>
        protected abstract void MakeRegistry();

    }
}
