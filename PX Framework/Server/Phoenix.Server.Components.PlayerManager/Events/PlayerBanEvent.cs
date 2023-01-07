using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Ban Event - Called when a player is banned
    /// </summary>
    public class PlayerBanEvent : AbstractPlayerEvent
    {
        private string? _reason;
        public PlayerBanEvent(GameServer server, Player player, string? reason) : base(server, player)
        {
            _reason = reason;
        }

        /// <summary>
        /// Retrieves the ban reason (can be undefined)
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
