using Phoenix.Common.Events;
using Phoenix.Common.Services;
using Phoenix.Server.Components;
using Phoenix.Server.Configuration;

namespace Phoenix.Server
{
    /// <summary>
    /// Component type more based around the basic game server
    /// </summary>
    public abstract class ServerComponent : Component
    {
        private AbstractConfigurationSegment config;
        private GameServer server;

        internal void PassGameServer(GameServer server)
        {
            if (this.server == null)
                this.server = server;
        }

        /// <summary>
        /// Defines the configuration key
        /// </summary>
        protected abstract string ConfigurationKey { get; }

        /// <summary>
        /// Retrieves the game server
        /// </summary>
        public GameServer Server
        {
            get
            {
                return server;
            }
        }

        /// <summary>
        /// Retrieves the service manager
        /// </summary>
        public ServiceManager ServiceManager
        {
            get
            {
                return server.ServiceManager;
            }
        }

        /// <summary>
        /// Retrieves the event bus
        /// </summary>
        public EventBus EventBus
        {
            get
            {
                return server.ServerEventBus;
            }
        }

        /// <summary>
        /// Component configuration
        /// </summary>
        public AbstractConfigurationSegment Configuration
        {
            get
            {
                if (config == null)
                {
                    config = Server.GetConfiguration(ConfigurationKey);
                }
                return config;
            }
        }
    }
}
