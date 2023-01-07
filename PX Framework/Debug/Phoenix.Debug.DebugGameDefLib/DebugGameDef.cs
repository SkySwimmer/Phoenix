using System.Net.NetworkInformation;
using Phoenix.Common;

namespace Phoenix.Debug.DebugGameDefLib
{
    /// <summary>
    /// Debug Phoenix Game Definition - Intended to be used with Newtonsoft.JSON
    /// </summary>
    public class DebugGameDef : Game
    {
        public string title;
        public string gameID;
        public string version;
        public string developmentStage;
        public bool hasOfflineSupport;

        private string assetsDir;
        private string baseDir;

        /// <summary>
        /// Assigns the game directories
        /// </summary>
        /// <param name="workingDir">Game working directory</param>
        /// <param name="assetsDir">Asset directory</param>
        public void SetDirectories(string workingDir, string assetsDir)
        {
            baseDir = workingDir;
            this.assetsDir = assetsDir;
        }

        /// <summary>
        /// Registers the definition as the active game implementation
        /// </summary>
        public void Register()
        {
            Implementation = this;
        }

        public override string GetAssetsFolder()
        {
            return assetsDir;
        }

        public override string GetDevelopmentStage()
        {
            return developmentStage;
        }

        public override string GetGameID()
        {
            return gameID;
        }

        public override string GetTitle()
        {
            return title;
        }

        public override string GetVersion()
        {
            return version;
        }

        public override bool HasOfflineSupport()
        {
            return hasOfflineSupport;
        }

        public override string GetGameFiles()
        {
            return baseDir;
        }

        public override string GetPlayerData()
        {
            return baseDir + "/PlayerData";
        }

        public override string GetSaveData()
        {
            return baseDir + "/SaveData";
        }

        public override string GetSessionToken()
        {
            return null;
        }

        public override bool IsCurrentlyOffline()
        {
            return !NetworkInterface.GetIsNetworkAvailable();
        }

        public override bool IsDebugMode()
        {
            return true;
        }
    }
}