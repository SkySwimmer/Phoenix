namespace Phoenix.Client.Events
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
    /// Client disconnected event - Called when the client disconnects
    /// </summary>
    public class ClientDisconnectedEvent : AbstractClientEvent
    {
        private ClientDisconnectedEventArgs _args;
        public ClientDisconnectedEvent(GameClient client, ClientDisconnectedEventArgs args) : base(client)
        {
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
    }
}
