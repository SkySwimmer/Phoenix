using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Unmute Event - Called when a player is unmuted
    /// </summary>
    public class PlayerUnmuteEvent : AbstractPlayerEvent
    {
        private string? _reason;
        public PlayerUnmuteEvent(GameServer server, Player player, string? reason) : base(server, player)
        {
            _reason = reason;
        }

        /// <summary>
        /// Retrieves the unmute reason (can be undefined)
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
