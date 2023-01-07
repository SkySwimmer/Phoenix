using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Mute Event - Called when a player is muted
    /// </summary>
    public class PlayerTempmuteEvent : AbstractPlayerEvent
    {
        private long _unmuteTimestamp;
        private string? _reason;
        public PlayerTempmuteEvent(GameServer server, long unmuteTimestamp, Player player, string? reason) : base(server, player)
        {
            _reason = reason;
            _unmuteTimestamp = unmuteTimestamp;
        }

        /// <summary>
        /// Retrieves the unmute timestamp
        /// </summary>
        public long UnmuteTimestamp
        {
            get
            {
                return _unmuteTimestamp;
            }
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
