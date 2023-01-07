using Phoenix.Common.Networking.Connections;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Client Connected Event - Called when a client connects to the server
    /// </summary>
    public class ClientConnectedEvent : AbstractServerEvent
    {
        private bool _keepConnected;
        private Connection _client;
        private ConnectionEventArgs _args;
        public ClientConnectedEvent(GameServer server, Connection client, ConnectionEventArgs args) : base(server)
        {
            _client = client;
            _args = args;
        }

        /// <summary>
        /// Retrieves the connection event args
        /// </summary>
        public ConnectionEventArgs EventArgs
        {
            get
            {
                return _args;
            }
        }

        /// <summary>
        /// Defines whether or not the client should remain open on event completion
        /// </summary>
        public bool ShouldKeepConnectionOpen
        {
            get
            {
                return _keepConnected;
            }
        }

        /// <summary>
        /// Prevents the server from closing the client when the event finishes
        /// </summary>
        public void KeepConnectionOpen()
        {
            _keepConnected = true;
        }

        /// <summary>
        /// Retrieves the client that connected
        /// </summary>
        public Connection Client
        {
            get
            {
                return _client;
            }
        }

        public override bool ShouldContinue()
        {
            return Client.IsConnected();
        }
    }
}
