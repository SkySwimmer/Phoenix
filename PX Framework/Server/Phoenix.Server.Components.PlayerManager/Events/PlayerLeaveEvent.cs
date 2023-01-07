using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Player Leave Event - Called when a player leaves the game
    /// </summary>
    public class PlayerLeaveEvent : AbstractPlayerEvent
    {
        private ClientDisconnectedEventArgs _args;
        public PlayerLeaveEvent(GameServer server, Player player, ClientDisconnectedEventArgs args) : base(server, player)
        {
            _args = args;
        }

        /// <summary>
        /// Retrieves the disconnect event arguments
        /// </summary>
        public ClientDisconnectedEventArgs Arguments
        {
            get
            {
                return _args;
            }
        }
    }
}
