using Phoenix.Common.Networking.Connections;

namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player join result object
    /// </summary>
    public class PlayerJoinResult
    {
        private Player _player;
        private PlayerJoinResultStatus _status;
        private DisconnectParams? _disconnectReason;

        public PlayerJoinResult(Player player, PlayerJoinResultStatus status, DisconnectParams? disconnectReason)
        {
            _player = player;
            _status = status;
            _disconnectReason = disconnectReason;
        }

        /// <summary>
        /// Retrieves the disconnect reason if present, null if not present
        /// </summary>
        public DisconnectParams? DisconnectReason
        {
            get 
            {
                return _disconnectReason;
            }
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
