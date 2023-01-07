using Phoenix.Common.Events;

namespace Phoenix.Client.Events
{
    /// <summary>
    /// Abstract server event
    /// </summary>
    public abstract class AbstractClientEvent : IEvent
    {
        private GameClient client;

        public AbstractClientEvent(GameClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Retrieves the game client
        /// </summary>
        public GameClient Client
        {
            get
            {
                return client;
            }
        }
    }
}
