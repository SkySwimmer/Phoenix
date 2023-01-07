using Phoenix.Common.Logging;
using Phoenix.Server;
using Phoenix.Unity.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.Unity
{
    /// <summary>
    /// Core binding utility for Phoenix games
    /// </summary>
    public static class CoreBindings
    {
        private static bool _loggingBound;
        private static bool _assetManagerBound;

        /// <summary>
        /// Attaches all bindings
        /// </summary>
        public static void BindAll()
        {
            // Bind logging
            BindLogging();

            // Bind asset manager
            BindAssetManager();
        }

        /// <summary>
        /// Attaches the logging bindings
        /// </summary>
        public static void BindLogging()
        {
            if (_loggingBound)
                return;
            PhoenixUnityLogBridge.Register();
            Logger.GetLogger("Phoenix Unity Bindings").Info("Successfully routed Phoenix logging to Unity!");
            _loggingBound = true;
        }

        /// <summary>
        /// Attaches the asset manager to unity
        /// </summary>
        public static void BindAssetManager()
        {
            if (_assetManagerBound)
                return;

            // Log
            Logger.GetLogger("Phoenix Unity Bindings").Info("Loading asset manager providers...");

            // Add providers
            AssetManager.AddProvider(new UnityAssetProvider());

            // Log
            Logger.GetLogger("Phoenix Unity Bindings").Info("Successfully bound the Phoenix Asset Manager to Unity!");

            // Finished
            _assetManagerBound = true;
        }
    }
}
