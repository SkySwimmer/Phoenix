using Phoenix.Common;
using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace Phoenix.Server
{
    /// <summary>
    /// Phoenix Server Asset Manager
    /// </summary>
    public static class AssetManager
    {
        private static bool locked = false;
        private static List<IAssetProvider> providers = new List<IAssetProvider>();
        private static Logger logger = Logger.GetLogger("Asset Manager");

        /// <summary>
        /// Adds asset providers
        /// </summary>
        /// <param name="provider">Asset provider to add</param>
        public static void AddProvider(IAssetProvider provider)
        {
            if (locked)
                throw new InvalidOperationException("Locked asset manager");
            providers.Add(provider);
        }

        /// <summary>
        /// Locks the asset manager
        /// </summary>
        public static void Lock()
        {
            locked = true;
        }

        /// <summary>
        /// Retrieves an array of asset names from a asset folder
        /// </summary>
        /// <param name="asset">Asset folder path</param>
        /// <returns>Array of asset names</returns>
        public static string[] GetAssetsIn(string asset)
        {
            if (asset.Contains("\\"))
            {
                if (Game.DebugMode)
                    logger.Warn("The asset manager is UNIX-based, using windows paths is not supported, attempting path conversion... (requested asset: " + asset + ") **THIS IS BAD PRACTICE**");
                asset = asset.Replace("\\", "/");
            }
            if (asset != "/" && !Regex.Match(asset, "^[0-9A-Za-z_\\-+=()][0-9A-Za-z./_\\-+= ()]*$").Success)
            {
                throw new ArgumentException("Invalid asset path");
            }
            if (Game.DebugMode)
            {
                if (asset.Contains("(") || asset.Contains(")") || asset.Contains(" ") || asset.Contains("+") || asset.Contains("=") || asset.Contains(" "))
                {
                    logger.Warn("Asset paths should only contain alphanumeric characters, dots, hyphens and underscores for code readability, found characters outside of this range in '" + asset + "'. **THIS IS BAD PRACTICE**");
                }
            }

            // Clean path
            while (asset.StartsWith("/"))
                asset = asset.Substring(1);
            while (asset.EndsWith("/"))
                asset = asset.Remove(asset.LastIndexOf("/"));
            while (asset.Contains("//"))
                asset = asset.Replace("//", "");

            List<string> assets = new List<string>();
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                IAssetProvider provider = providers[i];
                string[] arr = provider.GetAssetsIn(asset);
                assets.AddRange(arr);
            }
            return assets.ToArray();
        }

        /// <summary>
        /// Retrieves an asset data reader
        /// </summary>
        /// <param name="asset">Asset path</param>
        /// <param name="order">Asset scanning order</param>
        /// <returns>Asset data reader instance (closeable)</returns>
        public static CloseableDataReader GetAssetReader(string asset, AssetScanOrder order = AssetScanOrder.FRONT_TO_BACK)
        {
            return new CloseableDataReader(GetAssetStream(asset, order));
        }

        /// <summary>
        /// Retrieves a string asset
        /// </summary>
        /// <param name="asset">Asset path</param>
        /// <param name="order">Asset scanning order</param>
        /// <param name="encoding">Asset encoding (default is UTF-8)</param>
        /// <returns>Asset string</returns>
        public static string GetAssetString(string asset, AssetScanOrder order = AssetScanOrder.FRONT_TO_BACK, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            CloseableDataReader reader = GetAssetReader(asset, order);
            byte[] data = reader.ReadAllBytes();
            reader.Close();
            return encoding.GetString(data);
        }

        /// <summary>
        /// Retrieves assets as byte arrays
        /// </summary>
        /// <param name="asset">Asset path</param>
        /// <param name="order">Asset scanning order</param>
        /// <returns>Asset bytes</returns>
        public static byte[] GetAssetBytes(string asset, AssetScanOrder order = AssetScanOrder.FRONT_TO_BACK)
        {
            CloseableDataReader reader = GetAssetReader(asset, order);
            byte[] data = reader.ReadAllBytes();
            reader.Close();
            return data;
        }

        /// <summary>
        /// Retrieves an asset stream
        /// </summary>
        /// <param name="asset">Asset path</param>
        /// <param name="order">Asset scanning order</param>
        /// <returns>Asset stream instance</returns>
        public static Stream GetAssetStream(string asset, AssetScanOrder order = AssetScanOrder.FRONT_TO_BACK)
        {
            if (asset.Contains("\\"))
            {
                if (Game.DebugMode)
                    logger.Warn("The asset manager is UNIX-based, using windows paths is not supported, attempting path conversion... (requested asset: " + asset + ") **THIS IS BAD PRACTICE**");
                asset = asset.Replace("\\", "/");
            }
            if (!Regex.Match(asset, "^[0-9A-Za-z_\\-+=()][0-9A-Za-z./_\\-+= ()]*$").Success)
            {
                throw new ArgumentException("Invalid asset path");
            }
            if (Game.DebugMode)
            {
                if (asset.Contains("(") || asset.Contains(")") || asset.Contains(" ") || asset.Contains("+") || asset.Contains("=") || asset.Contains(" "))
                {
                    logger.Warn("Asset paths should only contain alphanumeric characters, dots, hyphens and underscores for code readability, found characters outside of this range in '" + asset + "'. **THIS IS BAD PRACTICE**");
                }
            }
        
            // Find asset
            while (asset.StartsWith("/"))
                asset = asset.Substring(1);
            while (asset.EndsWith("/"))
                asset = asset.Remove(asset.LastIndexOf("/"));
            while (asset.Contains("//"))
                asset = asset.Replace("//", "");
            logger.Debug("Loading asset: " + asset);
            if (order == AssetScanOrder.FRONT_TO_BACK)
            {
                for (int i = 0; i < providers.Count; i++)
                {
                    IAssetProvider provider = providers[i];
                    Stream? strm = provider.GetAssetStream(asset);
                    if (strm != null)
                        return strm;
                }
            }
            else
            {
                for (int i = providers.Count - 1; i >= 0; i--)
                {
                    IAssetProvider provider = providers[i];
                    Stream? strm = provider.GetAssetStream(asset);
                    if (strm != null)
                        return strm;
                }
            }
            throw new FileNotFoundException("Asset not found", asset);
        }

        /// <summary>
        /// Checks if an asset exists
        /// </summary>
        /// <param name="asset">Asset path</param>
        /// <returns>True if found, false otherwise</returns>
        public static bool AssetExists(string asset)
        {
            if (asset.Contains("\\"))
            {
                if (Game.DebugMode)
                    logger.Warn("The asset manager is UNIX-based, using windows paths is not supported, attempting path conversion... (requested asset: " + asset + ") **THIS IS BAD PRACTICE**");
                asset = asset.Replace("\\", "/");
            }
            if (!Regex.Match(asset, "^[0-9A-Za-z_\\-+=()][0-9A-Za-z./_\\-+= ()]*$").Success)
            {
                throw new ArgumentException("Invalid asset path");
            }
            if (Game.DebugMode)
            {
                if (asset.Contains("(") || asset.Contains(")") || asset.Contains(" ") || asset.Contains("+") || asset.Contains("=") || asset.Contains(" "))
                {
                    logger.Warn("Asset paths should only contain alphanumeric characters, dots, hyphens and underscores for code readability, found characters outside of this range in '" + asset + "'. **THIS IS BAD PRACTICE**");
                }
            }

            // Find asset
            while (asset.StartsWith("/"))
                asset = asset.Substring(1);
            while (asset.EndsWith("/"))
                asset = asset.Remove(asset.LastIndexOf("/"));
            while (asset.Contains("//"))
                asset = asset.Replace("//", "");

            for (int i = providers.Count - 1; i >= 0; i--)
            {
                IAssetProvider provider = providers[i];
                Stream? strm = provider.GetAssetStream(asset);
                if (strm != null)
                    return true;
            }
            return false;
        }
    }
}
