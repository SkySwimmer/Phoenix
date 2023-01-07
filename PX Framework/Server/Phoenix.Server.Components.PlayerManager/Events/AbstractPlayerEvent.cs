using Phoenix.Server.Players;

namespace Phoenix.Server.Events
{
    /// <summary>
    /// Abstract player event
    /// </summary>
    public abstract class AbstractPlayerEvent : AbstractServerEvent
    {
        private Player _player;
        public AbstractPlayerEvent(GameServer server, Player player) : base(server)
        {
            _player = player;
        }

        /// <summary>
        /// Retrieves the player instance
        /// </summary>
        public Player Player
        {
            get
            {
                return _player;
            }
        }
    }
}
