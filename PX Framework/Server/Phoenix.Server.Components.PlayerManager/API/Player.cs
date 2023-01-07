
using Phoenix.Common.Events;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Server.Events;

namespace Phoenix.Server.Players
{
    public class Player
    {
        private string _id;
        private string _displayName;
        private EventBus _eventBus;
        private GameServer _server;
        private Connection _client;
        private PlayerDataContainer _data;

        private List<object> objects = new List<object>();

        /// <summary>
        /// Event that is called on disconnect
        /// </summary>
        public event ConnectionDisconnectEventHandler? Disconnected;

        public Player(string id, string displayName, Connection client, GameServer server, EventBus eventBus)
        {
            _id = id;
            _displayName = displayName;
            _client = client;
            _eventBus = eventBus;
            _server = server;
            _data = _server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id);
            AddObject(server);
            client.Disconnected += (cl, reason, args) =>
            {
                Disconnected?.Invoke(cl, reason, args);
            };
        }

        /// <summary>
        /// Retrieves the player data container
        /// </summary>
        public PlayerDataContainer PlayerData
        {
            get
            {
                return _data;
            }
        }

        /// <summary>
        /// Retrieves the connection client
        /// </summary>
        public Connection Client
        {
            get
            {
                return _client;
            }
        }

        /// <summary>
        /// Retrieves the player display name
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        /// <summary>
        /// Retrieves the player ID
        /// </summary>
        public string PlayerID
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// Retrieves player objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <returns>Object instance or default (typically null)</returns>
        public T? GetObject<T>()
        {
            foreach (object obj in objects)
            {
                if (obj is T)
                    return (T)obj;
            }
            return Client.GetObject<T>();
        }

        /// <summary>
        /// Adds player objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object to add</param>
        public void AddObject<T>(T obj)
        {
            if (obj == null)
                return;
            T? old = GetObject<T>();
            if (old != null)
                objects.Remove(old);
            objects.Add(obj);
        }

        /// <summary>
        /// Kicks the player
        /// </summary>
        public void Kick()
        {
            _eventBus.Dispatch(new PlayerKickEvent(_server, this, null));
            if (Client.IsConnected())
                Client.Close("disconnect.kicked.undefined");
            Logger.GetLogger("player-manager").Info("Kicked " + _displayName);
        }

        /// <summary>
        /// Kicks the player
        /// </summary>
        /// <param name="reason">Kick reason</param>
        public void Kick(string reason)
        {
            _eventBus.Dispatch(new PlayerKickEvent(_server, this, reason));
            if (Client.IsConnected())
                Client.Close("disconnect.kicked", reason);
            Logger.GetLogger("player-manager").Info("Kicked " + _displayName + ": " + reason);
        }

        /// <summary>
        /// Checks if the player is banned
        /// </summary>
        public bool IsBanned
        {
            get
            {
                if (!_data.HasEntry("ban"))
                    return false;
                if (!_data.GetShard("ban").GetBool("temporary"))
                    return true;

                // Check unban time
                long unban = _data.GetShard("ban").GetLong("unbanat");
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > unban)
                {
                    _data.Delete("ban");
                    _data.Save();
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Checks if the player is muted
        /// </summary>
        public bool IsMuted
        {
            get
            {
                if (!_data.HasEntry("mute"))
                    return false;
                if (!_data.GetShard("mute").GetBool("temporary"))
                    return true;

                // Check unban time
                long unban = _data.GetShard("mute").GetLong("unmuteat");
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > unban)
                {
                    _data.Delete("mute");
                    _data.Save();
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban()
        {
            _eventBus.Dispatch(new PlayerUnbanEvent(_server, this, null));
            if (_data.HasEntry("ban"))
                _data.Delete("ban");
            Logger.GetLogger("player-manager").Info("Unbanned " + _displayName);
        }

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban(string reason)
        {
            _eventBus.Dispatch(new PlayerUnbanEvent(_server, this, reason));
            if (_data.HasEntry("ban"))
                _data.Delete("ban");
            Logger.GetLogger("player-manager").Info("Unbanned " + _displayName + ": " + reason);
        }

        /// <summary>
        /// Bans the player
        /// </summary>
        public void Ban()
        {
            _eventBus.Dispatch(new PlayerBanEvent(_server, this, null));
            if (!_data.HasEntry("ban"))
                _data.CreateShard("ban");
            _data.GetShard("ban").Set("temporary", false);
            _data.GetShard("ban").Set("hasreason", false);
            _data.Save();
            if (Client.IsConnected())
                Client.Close("disconnect.banned.undefined");
            Logger.GetLogger("player-manager").Info("Banned " + _displayName);
        }

        /// <summary>
        /// Bans the player
        /// </summary>
        /// <param name="reason">Ban reason</param>
        public void Ban(string reason)
        {
            _eventBus.Dispatch(new PlayerBanEvent(_server, this, reason));
            if (!_data.HasEntry("ban"))
                _data.CreateShard("ban");
            _data.GetShard("ban").Set("temporary", false);
            _data.GetShard("ban").Set("hasreason", true);
            _data.GetShard("ban").Set("reason", reason);
            _data.Save();
            if (Client.IsConnected())
                Client.Close("disconnect.banned", reason);
            Logger.GetLogger("player-manager").Info("Banned " + _displayName + ": " + reason);
        }

        /// <summary>
        /// Temporarily bans the player
        /// </summary>
        /// <param name="unbanTime">DateTime value when the ban will be removed</param>
        public void TempBan(DateTime unbanTime)
        {
            DateTimeOffset res = new DateTimeOffset(unbanTime.ToUniversalTime());
            _eventBus.Dispatch(new PlayerTempbanEvent(_server, res.ToUnixTimeMilliseconds(), this, null));
            if (!_data.HasEntry("ban"))
                _data.CreateShard("ban");
            _data.GetShard("ban").Set("temporary", true);
            _data.GetShard("ban").Set("hasreason", false);
            _data.GetShard("ban").Set("unbanat", res.ToUnixTimeMilliseconds());
            _data.Save();
            if (Client.IsConnected())
                Client.Close("disconnect.tempbanned.undefined", unbanTime.ToUniversalTime().ToLongDateString(), unbanTime.ToUniversalTime().ToShortTimeString());
            Logger.GetLogger("player-manager").Info("Temporarily banned " + _displayName + " until " + unbanTime.ToLongDateString() + " at " + unbanTime.ToShortTimeString());
        }

        /// <summary>
        /// Temporarily bans the player
        /// </summary>
        /// <param name="unbanTime">DateTime value when the ban will be removed</param>
        /// <param name="reason">Ban reason</param>
        public void TempBan(DateTime unbanTime, string reason)
        {
            DateTimeOffset res = new DateTimeOffset(unbanTime.ToUniversalTime());
            _eventBus.Dispatch(new PlayerTempbanEvent(_server, res.ToUnixTimeMilliseconds(), this, reason));
            if (!_data.HasEntry("ban"))
                _data.CreateShard("ban");
            _data.GetShard("ban").Set("temporary", true);
            _data.GetShard("ban").Set("hasreason", true);
            _data.GetShard("ban").Set("reason", reason);
            _data.GetShard("ban").Set("unbanat", res.ToUnixTimeMilliseconds());
            _data.Save();
            if (Client.IsConnected())
                Client.Close("disconnect.tempbanned", unbanTime.ToUniversalTime().ToLongDateString(), unbanTime.ToUniversalTime().ToShortTimeString(), reason);
            Logger.GetLogger("player-manager").Info("Temporarily banned " + _displayName + " until " + unbanTime.ToLongDateString() + " at " + unbanTime.ToShortTimeString() + ": " + reason);
        }

        /// <summary>
        /// Unmutes the player
        /// </summary>
        public void Unmute()
        {
            _eventBus.Dispatch(new PlayerUnmuteEvent(_server, this, null));
            if (_data.HasEntry("mute"))
                _data.Delete("mute");
            Logger.GetLogger("player-manager").Info("Unmuted " + _displayName);
        }

        /// <summary>
        /// Unmutes the player
        /// </summary>
        public void Unmute(string reason)
        {
            _eventBus.Dispatch(new PlayerUnmuteEvent(_server, this, reason));
            if (_data.HasEntry("mute"))
                _data.Delete("mute");
            Logger.GetLogger("player-manager").Info("Unmuted " + _displayName + ": " + reason);
        }

        /// <summary>
        /// Mutes the player
        /// </summary>
        public void Mute()
        {
            _eventBus.Dispatch(new PlayerMuteEvent(_server, this, null));
            if (!_data.HasEntry("mute"))
                _data.CreateShard("mute");
            _data.GetShard("mute").Set("temporary", false);
            _data.GetShard("mute").Set("hasreason", false);
            _data.Save();
            Logger.GetLogger("player-manager").Info("Muted " + _displayName);
        }

        /// <summary>
        /// Mutes the player
        /// </summary>
        /// <param name="reason">Mute reason</param>
        public void Mute(string reason)
        {
            _eventBus.Dispatch(new PlayerMuteEvent(_server, this, reason));
            if (!_data.HasEntry("mute"))
                _data.CreateShard("mute");
            _data.GetShard("mute").Set("temporary", false);
            _data.GetShard("mute").Set("hasreason", true);
            _data.GetShard("mute").Set("reason", reason);
            _data.Save();
            Logger.GetLogger("player-manager").Info("Muted " + _displayName + ": " + reason);
        }

        /// <summary>
        /// Temporarily mutes the player
        /// </summary>
        /// <param name="unmuteTime">DateTime value when the mute will be removed</param>
        public void TempMute(DateTime unmuteTime)
        {
            DateTimeOffset res = new DateTimeOffset(unmuteTime.ToUniversalTime());
            _eventBus.Dispatch(new PlayerTempmuteEvent(_server, res.ToUnixTimeMilliseconds(), this, null));
            if (!_data.HasEntry("mute"))
                _data.CreateShard("mute");
            _data.GetShard("mute").Set("temporary", true);
            _data.GetShard("mute").Set("hasreason", false);
            _data.GetShard("mute").Set("unmuteat", res.ToUnixTimeMilliseconds());
            _data.Save();
            Logger.GetLogger("player-manager").Info("Muted " + _displayName + " until " + unmuteTime.ToLongDateString() + " at " + unmuteTime.ToShortTimeString());
        }

        /// <summary>
        /// Temporarily mutes the player
        /// </summary>
        /// <param name="unmuteTime">DateTime value when the mute will be removed</param>
        /// <param name="reason">Ban reason</param>
        public void TempMute(DateTime unmuteTime, string reason)
        {
            DateTimeOffset res = new DateTimeOffset(unmuteTime.ToUniversalTime());
            _eventBus.Dispatch(new PlayerTempmuteEvent(_server, res.ToUnixTimeMilliseconds(), this, reason));
            if (!_data.HasEntry("mute"))
                _data.CreateShard("mute");
            _data.GetShard("mute").Set("temporary", true);
            _data.GetShard("mute").Set("hasreason", true);
            _data.GetShard("mute").Set("reason", reason);
            _data.GetShard("mute").Set("unmuteat", res.ToUnixTimeMilliseconds());
            _data.Save();
            Logger.GetLogger("player-manager").Info("Muted " + _displayName + " until " + unmuteTime.ToLongDateString() + " at " + unmuteTime.ToShortTimeString() + ": " + reason);
        }

        /// <summary>
        /// Ends the connection
        /// </summary>
        /// <param name="reason">Disconnect reason message</param>
        /// <param name="args">Message parameters</param>
        public void Disconnect(string reason, params string[] args)
        {
            if (Client.IsConnected())
                Client.Close(reason, args);
        }

        /// <summary>
        /// Ends the connection
        /// </summary>
        /// <param name="reason">Disconnect reason message</param>
        public void Disconnect(string reason)
        {
            Disconnect(reason, new string[0]);
        }

        /// <summary>
        /// Ends the connection
        /// </summary>
        public void Disconnect()
        {
            Disconnect("disconnect.generic");
        }

        /// <summary>
        /// Deletes the account
        /// </summary>
        public void DeleteData()
        {
            _server.ServiceManager.GetService<PlayerDataService>().DeletePlayerData(_id);
            Disconnect("disconnect.data.deleted");
        }

        /// <summary>
        /// Retrieves the player permission level
        /// </summary>
        /// <returns>Player permission level</returns>
        public PermissionLevel GetPermissionLevel()
        {
            return _server.ServiceManager.GetService<PermissionManagerService>().GetPermissionLevel(_data);
        }

        /// <summary>
        /// Assigns the player permission level
        /// </summary>
        /// <param name="level">New permission level</param>
        public void SetPermissionLevel(PermissionLevel level)
        {
            _server.ServiceManager.GetService<PermissionManagerService>().SetPermissionLevel(_data, level);
        }

        /// <summary>
        /// Retrieves the array of permissions granted to the player
        /// </summary>
        /// <returns>Array of permission keys</returns>
        public string[] GetAllowedPermissions()
        {
            return _server.ServiceManager.GetService<PermissionManagerService>().GetAllowedPermissions(_data);
        }

        /// <summary>
        /// Retrieves the array of permissions denied to the player
        /// </summary>
        /// <returns>Array of permission keys</returns>
        public string[] GetDeniedPermissions()
        {
            return _server.ServiceManager.GetService<PermissionManagerService>().GetDeniedPermissions(_data);
        }

        /// <summary>
        /// Adds permission nodes to the 'allowed' permission list
        /// </summary>
        /// <param name="key">Permission key</param>
        public void AddAllowedPermission(string key)
        {
            _server.ServiceManager.GetService<PermissionManagerService>().AddAllowedPermission(_data, key);
        }

        /// <summary>
        /// Adds permission nodes to the 'denied' permission list
        /// </summary>
        /// <param name="key">Permission key</param>
        public void AddDeniedPermission(string key)
        {
            _server.ServiceManager.GetService<PermissionManagerService>().AddDeniedPermission(_data, key);
        }

        /// <summary>
        /// Removes permission nodes from the 'allowed' permission list
        /// </summary>
        /// <param name="key">Permission key</param>
        public void RemoveAllowedPermission(string key)
        {
            _server.ServiceManager.GetService<PermissionManagerService>().RemoveAllowedPermission(_data, key);
        }

        /// <summary>
        /// Removes permission nodes from the 'denied' permission list
        /// </summary>
        /// <param name="key">Permission key</param>
        public void RemoveDeniedPermission(string key)
        {
            _server.ServiceManager.GetService<PermissionManagerService>().RemoveDeniedPermission(_data, key);
        }

        /// <summary>
        /// Checks permissions
        /// </summary>
        /// <param name="key">Permission key to check</param>
        /// <param name="level">Permission level to check in case the permission is not present</param>
        /// <returns>True if the player has the specified permission key, false otherwise</returns>
        public bool HasPermission(string key, PermissionLevel level)
        {
            return _server.ServiceManager.GetService<PermissionManagerService>().HasPermission(_data, key, level);
        }
    }
}
