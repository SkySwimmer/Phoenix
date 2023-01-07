using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Join Event - Called when a player joins
    /// </summary>
    public class PlayerJoinEvent : AbstractPlayerEvent
    {
        public PlayerJoinEvent(GameServer server, Player player) : base(server, player)
        {
        }
    }
}
