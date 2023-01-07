namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player Data Container
    /// </summary>
    public abstract class PlayerDataContainer : PlayerDataShard
    {
        /// <summary>
        /// Defines the player ID
        /// </summary>
        public abstract string PlayerID { get; }

        /// <summary>
        /// Defines the player data major format version
        /// </summary>
        public abstract int DataMajorVersion { get; set; }

        /// <summary>
        /// Defines the player data minor format version
        /// </summary>
        public abstract int DataMinorVersion { get; set; }

        /// <summary>
        /// Saves player data to disk
        /// </summary>
        public abstract void Save();
    }
}
