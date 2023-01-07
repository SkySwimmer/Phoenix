namespace Phoenix.Client.Events
{
    /// <summary>
    /// Client startup preparation event
    /// </summary>
    public class ClientStartupPrepareEvent : AbstractClientEvent
    {
        public ClientStartupPrepareEvent(GameClient client) : base(client)
        {
        }
    }
}
