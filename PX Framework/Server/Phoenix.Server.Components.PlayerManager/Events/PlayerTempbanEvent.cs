using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Ban Event - Called when a player is banned
    /// </summary>
    public class PlayerTempbanEvent : AbstractPlayerEvent
    {
        private long _unbanTimestamp;
        private string? _reason;
        public PlayerTempbanEvent(GameServer server, long unbanTimestamp, Player player, string? reason) : base(server, player)
        {
            _reason = reason;
            _unbanTimestamp = unbanTimestamp;
        }

        /// <summary>
        /// Retrieves the unban timestamp
        /// </summary>
        public long UnbanTimestamp
        {
            get
            {
                return _unbanTimestamp;
            }
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
