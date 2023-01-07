using Phoenix.Client.Components;
using Phoenix.Common.Events;
using Phoenix.Common.Services;

namespace Phoenix.Client
{
    /// <summary>
    /// Component type more based around the basic game client
    /// </summary>
    public abstract class ClientComponent : Component
    {
        private GameClient client;

        internal void PassGameClient(GameClient client)
        {
            if (this.client == null)
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

        /// <summary>
        /// Retrieves the service manager
        /// </summary>
        public ServiceManager ServiceManager
        {
            get
            {
                return client.ServiceManager;
            }
        }

        /// <summary>
        /// Retrieves the event bus
        /// </summary>
        public EventBus EventBus
        {
            get
            {
                return client.ClientEventBus;
            }
        }
    }
}
