using Phoenix.Common.Networking.Connections;

namespace Phoenix.Server.Events
{
    public class ClientDisconnectedEventArgs
    {
        private string _reason;
        private string[] _args;

        public ClientDisconnectedEventArgs(string reason, string[] args)
        {
            _reason = reason;
            _args = args;
        }

        /// <summary>
        /// Retrieves the disconnect reason key
        /// </summary>
        public string ReasonKey
        {
            get
            {
                return _reason;
            }
        }

        /// <summary>
        /// Retrieves the disconnect reason arguments
        /// </summary>
        public string[] ReasonArguments
        {
            get
            {
                return _args;
            }
        }
    }

    /// <summary>
    /// Client Disconnected Event - Called when a client disconnects from the server
    /// </summary>
    public class ClientDisconnectedEvent : AbstractServerEvent
    {
        private Connection _client;
        private ClientDisconnectedEventArgs _args;

        public ClientDisconnectedEvent(GameServer server, Connection client, ClientDisconnectedEventArgs args) : base(server)
        {
            _client = client;
            _args = args;
        }

        /// <summary>
        /// Retrieves the disconnect event arguments
        /// </summary>
        public ClientDisconnectedEventArgs Arguments
        {
            get
            {
                return _args;
            }
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
    }
}
