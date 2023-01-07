namespace Phoenix.Common.Networking.Connections
{
    /// <summary>
    /// Simple bundle of a client and server connection
    /// </summary>
    public class ConnectionBundle
    {
        private Connection _client;
        private ServerConnection _server;

        public ConnectionBundle(Connection client, ServerConnection server)
        {
            _client = client;
            _server = server;
        }

        /// <summary>
        /// Retrieves the client connection
        /// </summary>
        public Connection Client
        {
            get
            {
                return _client;
            }
        }

        /// <summary>
        /// Retrieves the server connection
        /// </summary>
        public ServerConnection Server
        {
            get
            {
                return _server;
            }
        }
    }
}
