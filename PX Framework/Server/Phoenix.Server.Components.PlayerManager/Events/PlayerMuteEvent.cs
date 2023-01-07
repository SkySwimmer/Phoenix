using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Mute Event - Called when a player is muted
    /// </summary>
    public class PlayerMuteEvent : AbstractPlayerEvent
    {
        private string? _reason;
        public PlayerMuteEvent(GameServer server, Player player, string? reason) : base(server, player)
        {
            _reason = reason;
        }

        /// <summary>
        /// Retrieves the mute reason (can be undefined)
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
