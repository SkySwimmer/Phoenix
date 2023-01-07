using Phoenix.Common.Events;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Server Stopped Event - Called when the server is shut down
    /// </summary>
    public class ServerStoppedEvent : AbstractServerEvent
    {
        public ServerStoppedEvent(GameServer server) : base(server)
        {
        }
    }
}
