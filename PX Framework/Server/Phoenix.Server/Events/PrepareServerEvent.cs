using Phoenix.Common.Events;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Prepare Server Event - Called when the server is being prepared for startup
    /// </summary>
    public class PrepareServerEvent : AbstractServerEvent
    {
        public PrepareServerEvent(GameServer server) : base(server)
        {
        }
    }
}
