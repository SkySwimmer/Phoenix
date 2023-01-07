using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Login Event - Called when a player logs in
    /// </summary>
    public class PlayerLoginEvent : AbstractPlayerEvent
    {
        public PlayerLoginEvent(GameServer server, Player player) : base(server, player)
        {
        }

        public override bool ShouldContinue()
        {
            return Player.Client.IsConnected();
        }
    }
}
