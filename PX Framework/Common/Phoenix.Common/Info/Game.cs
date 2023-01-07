namespace Phoenix.Common
{
    /// <summary>
    /// Phoenix Game Information Container
    /// </summary>
    public abstract class Game 
    {
        protected static Game Implementation = new NullImplementation();

        private class NullImplementation : Game
        {
            public override string GetAssetsFolder()
            {
                return null;
            }

            public override string GetDevelopmentStage()
            {
                return null;
            }

            public override string GetGameFiles()
            {
                return null;
            }

            public override string GetGameID()
            {
                return null;
            }

            public override string GetPlayerData()
            {
                return null;
            }

            public override string GetSaveData()
            {
                return null;
            }

            public override string GetSessionToken()
            {
                return null;
            }

            public override string GetTitle()
            {
                return null;
            }

            public override string GetVersion()
            {
                return null;
            }

            public override bool HasOfflineSupport()
            {
                return false;
            }

            public override bool IsCurrentlyOffline()
            {
                return true;
            }

            public override bool IsDebugMode()
            {
                return true;
            }
        }

        /// <summary>
        /// Game title
        /// </summary>
        public static string Title => Implementation.GetTitle();

        /// <summary>
        /// Game ID
        /// </summary>
        public static string GameID => Implementation.GetGameID();

        /// <summary>
        /// Current game version
        /// </summary>
        public static string Version => Implementation.GetVersion();

        /// <summary>
        /// Current game development stage
        /// </summary>
        public static string DevelopmentStage => Implementation.GetDevelopmentStage();

        /// <summary>
        /// Game session token (for contacting the login servers)
        /// </summary>
        public static string SessionToken => Implementation.GetSessionToken();

        /// <summary>
        /// Offline support, true if enabled, false if unsupported
        /// </summary>
        public static bool OfflineSupport => Implementation.HasOfflineSupport();

        /// <summary>
        /// Shows if the device is currently offline
        /// </summary>
        public static bool IsOffline => Implementation.IsCurrentlyOffline();

        /// <summary>
        /// Player data folder
        /// </summary>
        public static string PlayerData => Implementation.GetPlayerData();

        /// <summary>
        /// Save data folder
        /// </summary>
        public static string SaveData => Implementation.GetSaveData();

        /// <summary>
        /// Assets folder
        /// </summary>
        public static string AssetsFolder => Implementation.GetAssetsFolder();

        /// <summary>
        /// Game file folder
        /// </summary>
        public static string GameFiles => Implementation.GetGameFiles();

        /// <summary>
        /// Defines if the application is in debug mode
        /// </summary>
        public static bool DebugMode => Implementation.IsDebugMode();

        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetTitle();

        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetGameID();
        
        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetVersion();
        
        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetDevelopmentStage();
        
        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetSessionToken();
        
        /// <summary>
        /// Internal
        /// </summary> 
        public abstract bool HasOfflineSupport();
        
        /// <summary>
        /// Internal
        /// </summary> 
        public abstract bool IsCurrentlyOffline();

        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetPlayerData();

        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetSaveData();

        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetAssetsFolder();
        
        /// <summary>
        /// Internal
        /// </summary> 
        public abstract string GetGameFiles();

        /// <summary>
        /// Internal
        /// </summary> 
        public abstract bool IsDebugMode();
    }
}