using Phoenix.Common.Events;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Server Startup Completion Event
    /// </summary>
    public class ServerStartupCompletedEvent : AbstractServerEvent
    {
        public ServerStartupCompletedEvent(GameServer server) : base(server)
        {
        }
    }
}
