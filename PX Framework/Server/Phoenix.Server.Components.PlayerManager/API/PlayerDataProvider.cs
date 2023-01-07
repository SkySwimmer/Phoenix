namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player Data Provider
    /// </summary>
    public interface PlayerDataProvider
    {
        /// <summary>
        /// Checks if this provider has data for the given ID
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>True if this provider has data for the given player, false otherwise</returns>
        public bool HasPlayerData(string id);

        /// <summary>
        /// Provides player data
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>Player data container</returns>
        public PlayerDataContainer Provide(string id);

        /// <summary>
        /// Deletes player data
        /// </summary>
        /// <param name="id">Player ID</param>
        public void DeletePlayerData(string id);

        /// <summary>
        /// Defines the current major data version
        /// </summary>
        /// <returns>Current major data version</returns>
        public int GetCurrentMajorDataVersion();

        /// <summary>
        /// Defines the current minor data version
        /// </summary>
        /// <returns>Current minor data version</returns>
        public int GetCurrentMinorDataVersion();

        /// <summary>
        /// Defines whether or not this provider can create data containers in case none of the providers have data to provide
        /// </summary>
        /// <returns>True if this provider can work like a fallback, false otherwise</returns>
        public bool CanUseAsFallback();
    }
}
