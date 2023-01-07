namespace Phoenix.Client.Events
{
    /// <summary>
    /// Client connected event - Called when the connection is established
    /// </summary>
    public class ClientConnectedEvent : AbstractClientEvent
    {
        public ClientConnectedEvent(GameClient client) : base(client)
        {
        }
    }
}
