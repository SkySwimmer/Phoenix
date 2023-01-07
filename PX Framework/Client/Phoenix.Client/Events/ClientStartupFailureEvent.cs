namespace Phoenix.Client.Events
{
    /// <summary>
    /// Client startup failure event
    /// </summary>
    public class ClientStartupFailureEvent : AbstractClientEvent
    {
        private ClientStartFailureType errorType;
        public ClientStartupFailureEvent(ClientStartFailureType errorType, GameClient client) : base(client)
        {
            this.errorType = errorType;
        }

        /// <summary>
        /// Error type
        /// </summary>
        public ClientStartFailureType ErrorType
        {
            get
            {
                return errorType;
            }
        }
    }
}
