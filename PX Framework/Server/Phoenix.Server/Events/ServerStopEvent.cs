using Phoenix.Common.Events;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Server Stop Event - Called when the server is shutting down
    /// </summary>
    public class ServerStopEvent : AbstractServerEvent
    {
        public ServerStopEvent(GameServer server) : base(server)
        {
        }
    }
}
