using Phoenix.Common.Logging;
using Phoenix.Unity.Bindings.AssetCompilers;
using System.Collections.Generic;
using System.IO;

namespace Phoenix.Unity.Bindings
{
    /// <summary>
    /// Server asset compiler
    /// </summary>
    public static class AssetCompiler
    {
        private static Logger Logger = Logger.GetLogger("Asset Compiler");
        private static List<IAssetCompiler> Compilers = new List<IAssetCompiler>()
        {
#if !UNITY_STANDALONE
            new ChartCompiler()
#endif
        };

        /// <summary>
        /// Adds asset compilers
        /// </summary>
        /// <param name="compiler">Compiler to add</param>
        public static void AddCompiler(IAssetCompiler compiler)
        {
            Compilers.Add(compiler);
        }

        /// <summary>
        /// Compiler log level
        /// </summary>
        public static LogLevel LogLevel = LogLevel.DEBUG;

        /// <summary>
        /// Compiles an asset
        /// </summary>
        /// <param name="asset">Asset stream</param>
        /// <param name="file">Asset name</param>
        /// <returns>Compiled asset stream</returns>
        public static Stream Compile(Stream asset, string file)
        {
            foreach (IAssetCompiler compiler in Compilers)
            {
                if (file.EndsWith("." + compiler.FileExtension))
                {
                    Logger.Log(LogLevel, "Compiling: " + file);
                    return compiler.Compile(asset, file);
                }
            }
            return asset;
        }
        
    }
}
