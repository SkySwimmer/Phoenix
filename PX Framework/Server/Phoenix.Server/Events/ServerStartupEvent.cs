using Phoenix.Common.Events;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Start Server Event - Called when the server is starting
    /// </summary>
    public class ServerStartupEvent : AbstractServerEvent
    {
        public ServerStartupEvent(GameServer server) : base(server)
        {
        }
    }
}
