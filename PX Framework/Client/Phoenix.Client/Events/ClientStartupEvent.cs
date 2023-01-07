namespace Phoenix.Client.Events
{
    /// <summary>
    /// Client startup event - Called early in the startup process
    /// </summary>
    public class ClientStartupEvent : AbstractClientEvent
    {
        public ClientStartupEvent(GameClient client) : base(client)
        {
        }
    }
}
