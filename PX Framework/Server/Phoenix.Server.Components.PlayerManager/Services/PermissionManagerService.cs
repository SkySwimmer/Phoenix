using Phoenix.Common.Services;

namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player Permission Management Service
    /// </summary>
    public class PermissionManagerService : IService
    {
        private bool _enabled = true;

        /// <summary>
        /// Defines whether or not the permission manager is enabled
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }

        private bool _grantOperatorToOwner = true;

        /// <summary>
        /// Defines whether or not the permission manager grants operator permissions to the server owner
        /// </summary>
        public bool ShouldGrantOperatorPermissionsToOwner
        {
            get
            {
                return _grantOperatorToOwner;
            }
            set
            {
                _grantOperatorToOwner = value;
            }
        }

        private GameServer _server;
        public PermissionManagerService(GameServer server)
        {
            _server = server;
        }

        /// <summary>
        /// Assigns the permission level of a player
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <param name="level">New permission level</param>
        public void SetPermissionLevel(string id, PermissionLevel level)
        {
            SetPermissionLevel(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id), level);
        }

        /// <summary>
        /// Assigns the permission level of a player
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <param name="level">New permission level</param>
        public void SetPermissionLevel(PlayerDataContainer data, PermissionLevel level)
        {
            if (!Enabled)
                return;

            // Check permission shard
            if (!data.HasEntry("permissions"))
            {
                // Create shard
                PlayerDataShard s = data.CreateShard("permissions");
                s.Set("level", level);
                s.Set("allowed", new string[0]);
                s.Set("denied", new string[0]);
                data.Save();
            }
            else
            {
                // Update shard
                PlayerDataShard shard = data.GetShard("permissions");
                shard.Set("level", level);
                data.Save();
            }
        }

        /// <summary>
        /// Retrieves the player permission level
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>Player permission level</returns>
        public PermissionLevel GetPermissionLevel(string id)
        {
            return GetPermissionLevel(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id));
        }

        /// <summary>
        /// Retrieves the player permission level
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <returns>Player permission level</returns>
        public PermissionLevel GetPermissionLevel(PlayerDataContainer data)
        {
            if (!Enabled)
                return PermissionLevel.DEFAULT;
            Components.PlayerManagerComponent comp = _server.GetComponent<Components.PlayerManagerComponent>();
            if (comp.OwnerPlayerID != null && data.PlayerID == comp.ID)
                return PermissionLevel.OPERATOR;

            if (data.HasEntry("permissions"))
                return data.GetShard("permissions").GetEnum<PermissionLevel>("level");
            return PermissionLevel.DEFAULT;
        }

        /// <summary>
        /// Retrieves the array of permissions granted to the player
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>Array of permission keys</returns>
        public string[] GetAllowedPermissions(string id)
        {
            return GetAllowedPermissions(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id));
        }

        /// <summary>
        /// Retrieves the array of permissions granted to the player
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <returns>Array of permission keys</returns>
        public string[] GetAllowedPermissions(PlayerDataContainer data)
        {
            if (!Enabled)
                return new string[0];

            if (data.HasEntry("permissions"))
                return data.GetShard("permissions").GetStringArray("allowed");
            return new string[0];
        }

        /// <summary>
        /// Retrieves the array of permissions denied to the player
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>Array of permission keys</returns>
        public string[] GetDeniedPermissions(string id)
        {
            return GetDeniedPermissions(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id));
        }

        /// <summary>
        /// Retrieves the array of permissions denied to the player
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <returns>Array of permission keys</returns>
        public string[] GetDeniedPermissions(PlayerDataContainer data)
        {
            if (!Enabled)
                return new string[0];

            if (data.HasEntry("permissions"))
                return data.GetShard("permissions").GetStringArray("denied");
            return new string[0];
        }

        /// <summary>
        /// Adds permission nodes to the 'allowed' permission list
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <param name="key">Permission key</param>
        public void AddAllowedPermission(string id, string key)
        {
            AddAllowedPermission(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id), key);
        }

        /// <summary>
        /// Adds permission nodes to the 'allowed' permission list
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <param name="key">Permission key</param>
        public void AddAllowedPermission(PlayerDataContainer data, string key)
        {
            if (!Enabled)
                return;

            // Check permission shard
            if (!data.HasEntry("permissions"))
            {
                // Create shard
                PlayerDataShard s = data.CreateShard("permissions");
                s.Set("level", PermissionLevel.DEFAULT);
                s.Set("allowed", new string[] { key });
                s.Set("denied", new string[0]);
                data.Save();
            }
            else
            {
                // Retrieve list
                List<string> perms = new List<string>(GetAllowedPermissions(data));
                if (!perms.Contains(key))
                {
                    perms.Add(key);

                    // Update shard
                    PlayerDataShard shard = data.GetShard("permissions");
                    shard.Set("allowed", perms.ToArray());
                    data.Save();
                }
            }
        }

        /// <summary>
        /// Removes permission nodes from the 'allowed' permission list
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <param name="key">Permission key</param>
        public void RemoveAllowedPermission(string id, string key)
        {
            RemoveAllowedPermission(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id), key);
        }

        /// <summary>
        /// Removes permission nodes from the 'allowed' permission list
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <param name="key">Permission key</param>
        public void RemoveAllowedPermission(PlayerDataContainer data, string key)
        {
            if (!Enabled)
                return;

            // Check permission shard
            if (data.HasEntry("permissions"))
            {
                // Retrieve list
                List<string> perms = new List<string>(GetAllowedPermissions(data));
                if (perms.Contains(key))
                {
                    perms.Remove(key);

                    // Update shard
                    PlayerDataShard shard = data.GetShard("permissions");
                    shard.Set("allowed", perms.ToArray());
                    data.Save();
                }
            }
        }

        /// <summary>
        /// Adds permission nodes to the 'denied' permission list
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <param name="key">Permission key</param>
        public void AddDeniedPermission(string id, string key)
        {
            AddDeniedPermission(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id), key);
        }

        /// <summary>
        /// Adds permission nodes to the 'denied' permission list
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <param name="key">Permission key</param>
        public void AddDeniedPermission(PlayerDataContainer data, string key)
        {
            if (!Enabled)
                return;

            // Check permission shard
            if (!data.HasEntry("permissions"))
            {
                // Create shard
                PlayerDataShard s = data.CreateShard("permissions");
                s.Set("level", PermissionLevel.DEFAULT);
                s.Set("allowed", new string[0]);
                s.Set("denied", new string[] { key });
                data.Save();
            }
            else
            {
                // Retrieve list
                List<string> perms = new List<string>(GetDeniedPermissions(data));
                if (!perms.Contains(key))
                {
                    perms.Add(key);

                    // Update shard
                    PlayerDataShard shard = data.GetShard("permissions");
                    shard.Set("denied", perms.ToArray());
                    data.Save();
                }
            }
        }

        /// <summary>
        /// Removes permission nodes from the 'denied' permission list
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <param name="key">Permission key</param>
        public void RemoveDeniedPermission(string id, string key)
        {
            RemoveDeniedPermission(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id), key);
        }

        /// <summary>
        /// Removes permission nodes from the 'denied' permission list
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <param name="key">Permission key</param>
        public void RemoveDeniedPermission(PlayerDataContainer data, string key)
        {
            if (!Enabled)
                return;

            // Check permission shard
            if (data.HasEntry("permissions"))
            {
                // Retrieve list
                List<string> perms = new List<string>(GetDeniedPermissions(data));
                if (perms.Contains(key))
                {
                    perms.Remove(key);

                    // Update shard
                    PlayerDataShard shard = data.GetShard("permissions");
                    shard.Set("denied", perms.ToArray());
                    data.Save();
                }
            }
        }

        /// <summary>
        /// Checks if the player has the specified permission
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <param name="key">Permission key to check</param>
        /// <param name="level">Permission level to check in case the permission is not present</param>
        /// <returns>True if the player has the specified permission key, false otherwise</returns>
        public bool HasPermission(string id, string key, PermissionLevel level)
        {
            return HasPermission(_server.ServiceManager.GetService<PlayerDataService>().GetPlayerData(id), key, level);
        }

        /// <summary>
        /// Checks if the player has the specified permission
        /// </summary>
        /// <param name="data">Player data container</param>
        /// <param name="key">Permission key to check</param>
        /// <param name="level">Permission level to check in case the permission is not present</param>
        /// <returns>True if the player has the specified permission key, false otherwise</returns>
        public bool HasPermission(PlayerDataContainer data, string key, PermissionLevel level)
        {
            if (!Enabled)
                return level == PermissionLevel.DEFAULT;
            Components.PlayerManagerComponent comp = _server.GetComponent<Components.PlayerManagerComponent>();
            if (comp.OwnerPlayerID != null && ShouldGrantOperatorPermissionsToOwner && data.PlayerID == comp.ID)
                return true;
            string[] pRaw = key.Split('.');
            string[] parts = new string[pRaw.Length];
            string last = "";
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = last + pRaw[i];
                last = parts[i] + ".";
            }

            // Check permission shard
            if (!data.HasEntry("permissions"))
            {
                PlayerDataShard s = data.CreateShard("permissions");
                s.Set("level", PermissionLevel.DEFAULT);
                s.Set("allowed", new string[0]);
                s.Set("denied", new string[0]);
                data.Save();
            }

            // Load shard
            PlayerDataShard shard = data.GetShard("permissions");
            PermissionLevel playerLevel = shard.GetEnum<PermissionLevel>("level");
            string[] permissionsAllowed = shard.GetStringArray("allowed");
            string[] permissionsDenied = shard.GetStringArray("denied");

            // Check allowed permissions (exact only)
            foreach (string perm in permissionsAllowed)
            {
                if (perm == key)
                    return true;
            }

            // Check denied permissions
            foreach (string perm in permissionsDenied)
            {
                string permission = perm;
                if (perm.EndsWith(".*"))
                    permission = permission.Remove(permission.LastIndexOf(".*"));
                foreach (string part in parts)
                {
                    if (part == key)
                        return false;
                }
            }

            // Check allowed permissions
            foreach (string perm in permissionsAllowed)
            {
                string permission = perm;
                if (perm.EndsWith(".*"))
                    permission = permission.Remove(permission.LastIndexOf(".*"));
                foreach (string part in parts)
                {
                    if (part == key)
                        return true;
                }
            }

            // Check level
            return playerLevel >= level;
        }

    }
}
