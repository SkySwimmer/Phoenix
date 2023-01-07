using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Kick Event - Called when a player is kicked
    /// </summary>
    public class PlayerKickEvent : AbstractPlayerEvent
    {
        private string? _reason;
        public PlayerKickEvent(GameServer server, Player player, string? reason) : base(server, player)
        {
            _reason = reason;
        }

        /// <summary>
        /// Retrieves the kick reason (can be undefined)
        /// </summary>
        public string? Reason
        {
            get
            {
                return _reason;
            }
        }
    }
}
