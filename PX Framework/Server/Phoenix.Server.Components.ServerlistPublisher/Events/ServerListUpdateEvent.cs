using Phoenix.Common.Events;
using Phoenix.Server.Components.ServerlistPublisher;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Server List Update Event - Called to add/update detail entries in the server list
    /// </summary>
    public class ServerListUpdateEvent : AbstractServerEvent
    {
        private ServerListDetails _details;
        public ServerListUpdateEvent(GameServer server, ServerListDetails details) : base(server)
        {
            _details = details;
        }

        /// <summary>
        /// Retrieves the server list detail block
        /// </summary>
        public ServerListDetails DetailBlock
        {
            get
            {
                return _details;
            }
        }
    }
}
