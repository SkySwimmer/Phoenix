using Phoenix.Common.Events;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Abstract server event
    /// </summary>
    public abstract class AbstractServerEvent : IEvent
    {
        private GameServer server;

        public AbstractServerEvent(GameServer server)
        {
            this.server = server;
        }

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
    }
}
