namespace Phoenix.Client.Events
{
    /// <summary>
    /// Client disconnect completion event - Called when the client is fully stopped and cleaned up
    /// </summary>
    public class ClientDisconnectCompleteEvent : AbstractClientEvent
    {
        private ClientDisconnectedEventArgs _args;
        public ClientDisconnectCompleteEvent(GameClient client, ClientDisconnectedEventArgs args) : base(client)
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
