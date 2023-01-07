namespace Phoenix.Server
{
    /// <summary>
    /// Asset provider interface for the Phoenix server asset manager
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// Retrieves an asset stream
        /// </summary>
        /// <param name="asset">Asset path</param>
        /// <returns>Stream instance or null</returns>
        public Stream? GetAssetStream(string asset);

        /// <summary>
        /// Retrieves an array of assets in a folder
        /// </summary>
        /// <param name="folder">Asset folder path</param>
        /// <returns>Array of asset names</returns>
        public string[] GetAssetsIn(string folder);
    }
}
