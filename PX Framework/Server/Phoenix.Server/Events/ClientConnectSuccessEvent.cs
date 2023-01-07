using Phoenix.Common.Networking.Connections;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Client Connection Success Event - Called when a client connects to the server, after the connection packet handlers are started
    /// </summary>
    public class ClientConnectSuccessEvent : AbstractServerEvent
    {
        private Connection _client;
        public ClientConnectSuccessEvent(GameServer server, Connection client) : base(server)
        {
            _client = client;
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
