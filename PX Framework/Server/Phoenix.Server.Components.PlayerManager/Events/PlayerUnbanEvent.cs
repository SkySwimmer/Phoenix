using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Unban Event - Called when a player is unbanned
    /// </summary>
    public class PlayerUnbanEvent : AbstractPlayerEvent
    {
        private string? _reason;
        public PlayerUnbanEvent(GameServer server, Player player, string? reason) : base(server, player)
        {
            _reason = reason;
        }

        /// <summary>
        /// Retrieves the unban reason (can be undefined)
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
