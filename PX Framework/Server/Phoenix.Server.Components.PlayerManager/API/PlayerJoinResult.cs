namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player join result object
    /// </summary>
    public class PlayerJoinResult
    {
        private Player _player;
        private PlayerJoinResultStatus _status;

        public PlayerJoinResult(Player player, PlayerJoinResultStatus status)
        {
            _player = player;
            _status = status;
        }

        /// <summary>
        /// Checks if the player was successfully added
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                return _status == PlayerJoinResultStatus.SUCCESS;
            }
        }

        /// <summary>
        /// Player instance
        /// </summary>
        public Player Player
        {
            get
            {
                return _player;
            }
        }

        /// <summary>
        /// Join result status
        /// </summary>
        public PlayerJoinResultStatus Status 
        {
            get
            {
                return _status;
            }
        }
    }

    public enum PlayerJoinResultStatus
    { 
        SUCCESS,
        GENERIC_FAILURE,
        BANNED,
        TEMPBANNED,
        SERVER_FULL
    }

}
