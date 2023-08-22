using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Services;
using Phoenix.Server.Configuration;
using Phoenix.Server.Events;

namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player Management Service
    /// </summary>
    public class PlayerManagerService : IService
    {
        private GameServer _server;
        private Dictionary<string, Player> _players = new Dictionary<string, Player>();

        public PlayerManagerService(GameServer server)
        {
            _server = server;
        }

        private bool _overrideEnablePlayerLimit = false;
        private bool _enablePlayerLimit = true;
        private bool _overrideMaxPlayers = false;
        private int _maxPlayers = 0;
        private bool _enableGameMaxPlayers = false;
        private int _gameMaxPlayers = 50;

        internal void Configure(AbstractConfigurationSegment config)
        {
            if (!config.HasEntry("enablelimit") && !_overrideEnablePlayerLimit)
                config.SetBool("enablelimit", true);
            if (!_overrideEnablePlayerLimit)
                _enablePlayerLimit = config.GetBool("enablelimit");
            if (!config.HasEntry("playerlimit") && !_overrideEnablePlayerLimit)
                config.SetInteger("playerlimit", _gameMaxPlayers);
            if (!_overrideMaxPlayers)
                _maxPlayers = config.GetInteger("playerlimit");
            if (EnablePlayerLimit)
            {
                if (_maxPlayers <= 0 || (_enableGameMaxPlayers && _maxPlayers > _gameMaxPlayers))
                {
                    config.SetInteger("playerlimit", _gameMaxPlayers);
                    Logger.GetLogger("player-manager").Warn("Invalid player limit: " + _maxPlayers + ", resetted to " + _gameMaxPlayers);
                }
            }
        }

        /// <summary>
        /// Adds a player after login
        /// </summary>
        /// <param name="client">Player client</param>
        /// <param name="playerID">Player ID</param>
        /// <param name="displayName">Player Display Name</param>
        /// <returns>PlayerJoinResult object</returns>
        public PlayerJoinResult AddPlayer(Connection client, string playerID, string displayName)
        {
            // Call login
            Player plr = new Player(playerID, displayName, client, _server, _server.ServerEventBus);
            Logger.GetLogger("player-manager").Info("Player login " + playerID + " from " + client.GetRemoteAddress() + " (logging in as " + displayName + ")");
            _server.ServerEventBus.Dispatch(new PlayerLoginEvent(_server, plr));
            if (!client.IsConnected())
                return new PlayerJoinResult(plr, PlayerJoinResultStatus.GENERIC_FAILURE, new DisconnectParams("connection.lost"));

            // Check player limit
            if (EnablePlayerLimit && Players.Length >= PlayerLimit)
            {
                // Check permissions
                if (!plr.HasPermission("loginoverride.maxplayers", PermissionLevel.MODERATOR))
                {
                    // Disconnect
                    Logger.GetLogger("player-manager").Info("Player login failure: " + displayName + " could not log in: server is full.");
                    return new PlayerJoinResult(plr, PlayerJoinResultStatus.SERVER_FULL, new DisconnectParams("disconnect.loginfailure.fullserver"));
                }
            }

            // Check ban
            if (plr.IsBanned)
            {
                // Check tempban
                bool tempBan = plr.PlayerData.GetShard("ban").GetBool("temporary");
                bool hasReason = plr.PlayerData.GetShard("ban").GetBool("hasreason");
                if (!hasReason)
                    Logger.GetLogger("player-manager").Info("Player login failure: " + displayName + " could not log in: player is banned.");
                else
                    Logger.GetLogger("player-manager").Info("Player login failure: " + displayName + " could not log in: player is banned: " + plr.PlayerData.GetShard("ban").GetString("reason"));
                if (!tempBan)
                {
                    if (!hasReason)
                        return new PlayerJoinResult(plr, PlayerJoinResultStatus.BANNED, new DisconnectParams("disconnect.loginfailure.banned.undefined"));
                    else
                        return new PlayerJoinResult(plr, PlayerJoinResultStatus.BANNED, new DisconnectParams("disconnect.loginfailure.banned", plr.PlayerData.GetShard("ban").GetString("reason")));
                }
                else
                {
                    long unban = plr.PlayerData.GetShard("ban").GetLong("unbanat");
                    DateTimeOffset unbanTime = DateTimeOffset.FromUnixTimeSeconds(unban);
                    if (!hasReason)
                        return new PlayerJoinResult(plr, PlayerJoinResultStatus.TEMPBANNED, new DisconnectParams("disconnect.loginfailure.tempbanned.undefined", unbanTime.DateTime.ToLongDateString(), unbanTime.DateTime.ToShortTimeString()));
                    else
                        return new PlayerJoinResult(plr, PlayerJoinResultStatus.TEMPBANNED, new DisconnectParams("disconnect.loginfailure.tempbanned", unbanTime.DateTime.ToLongDateString(), unbanTime.DateTime.ToShortTimeString(), plr.PlayerData.GetShard("ban").GetString("reason")));
                }
            }

            // Disconnect already-connected players
            Player? old = GetOnlinePlayerOrNull(playerID);
            if (old != null)
                old.Disconnect("disconnect.duplicatelogin");

            // Add player
            _players[playerID] = plr;
            Logger.GetLogger("player-manager").Info("Player connected: " + displayName + " (" + playerID + ")");

            // Bind events
            client.Disconnected += (client, reason, args) =>
            {
                Logger.GetLogger("player-manager").Info("Player disconnected: " + displayName + " (" + playerID + "): " + reason);
                _server.ServerEventBus.Dispatch(new PlayerLeaveEvent(_server, plr, new ClientDisconnectedEventArgs(reason, args)));
                _players.Remove(playerID);
            };
            client.AddObject(plr);
            return new PlayerJoinResult(plr, PlayerJoinResultStatus.SUCCESS, null);
        }

        /// <summary>
        /// Defines the limit of the max-players property, if the user-configured value of this property is higher than this field, it will be lowered to the value of this field. (disabled unless assigned)
        /// </summary>
        public int GamePlayerLimit
        {
            get
            {
                return _gameMaxPlayers;
            }
            set
            {
                _gameMaxPlayers = value;
                _enableGameMaxPlayers = true;
            }
        }

        /// <summary>
        /// Defines whether or not the player limit is enabled
        /// </summary>
        public bool EnablePlayerLimit
        {
            get
            {
                return _enablePlayerLimit;
            }
            set
            {
                _enablePlayerLimit = value;
                _overrideEnablePlayerLimit = true;
            }
        }

        /// <summary>
        /// Defines the server player limit
        /// </summary>
        public int PlayerLimit
        {
            get
            {
                return _maxPlayers;
            }
            set
            {
                _maxPlayers = value;
                _overrideMaxPlayers = true;
            }
        }

        /// <summary>
        /// Retrieves an array of all connected players
        /// </summary>
        public Player[] Players
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _players.Values.ToArray();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Retrieves online players by ID
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>Player instance</returns>
        public Player GetOnlinePlayer(string id)
        {
            if (!_players.ContainsKey(id))
                throw new ArgumentException("Player not found");
            return _players[id];
        }

        /// <summary>
        /// Checks if a player is online
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>True if the player is online, false otherwise</returns>
        public bool IsOnline(string id)
        {
            return _players.ContainsKey(id);
        }

        /// <summary>
        /// Retrieves online players by ID
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>Player instance</returns>
        public Player? GetOnlinePlayerOrNull(string id)
        {
            if (!_players.ContainsKey(id))
                return null;
            return _players[id];
        }

    }
}
