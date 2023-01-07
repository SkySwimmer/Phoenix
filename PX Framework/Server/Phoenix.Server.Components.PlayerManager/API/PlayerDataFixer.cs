namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player Data Fixer - Used to update player data before fully loading it.
    /// <br/>
    /// <br/>
    /// The concept of Phoenix Data Fixers works like this:
    /// <br/>
    /// - Each time player data is loaded, the major and minor version is compared to the current expected version of game data.
    /// <br/>
    /// - If it doesn't match, the data manager will go through all data fixers and find the ones matching the CURRENT data version. (<b>this is the version currently used in the on-disk player data</b>)
    /// <br/>
    /// - Each data fixer that matches the major and minor version of the current data version will be run. After each fixer completes, the data manager will move to the next minor version.
    /// <br/>
    /// - If there are no data fixers for the current minor version, the data manager will move the save data to the next major version and minor version 0.
    /// <br/>
    /// - This process repeats until the data has been updated, after that, data is flushed to disk before loading it into the game.
    /// </summary>
    public abstract class PlayerDataFixer
    {
        /// <summary>
        /// Defines the major version to run this fixer on
        /// </summary>
        public abstract int DataVersionMajor { get; }

        /// <summary>
        /// Defines the minor version to run this fixer on
        /// </summary>
        public virtual int DataVersionMinor => 0;

        /// <summary>
        /// Updates player data
        /// </summary>
        /// <param name="data">Data container</param>
        public abstract void Fix(PlayerDataContainer data);
    }
}
