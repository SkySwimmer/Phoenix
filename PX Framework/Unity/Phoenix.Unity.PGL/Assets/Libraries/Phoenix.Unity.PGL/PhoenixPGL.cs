using Phoenix.Common.Logging;
using GameObject = UnityEngine.GameObject;
using Application = UnityEngine.Application;
using PlayerPrefs = UnityEngine.PlayerPrefs;
using RuntimePlatform = UnityEngine.RuntimePlatform;
using Debug = UnityEngine.Debug;
using Phoenix.Server;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Phoenix.Common;
using Phoenix.Client;
using Phoenix.Client.Authenticators.PhoenixAPI;
using Phoenix.Unity.PGL.Internal;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using Phoenix.Unity.PGL.Internal.Packages;
using Phoenix.Unity.PGL.Mods;
using System.IO.Compression;
using Phoenix.Client.Components;
using System.Security.Cryptography;
using System.Linq;
using Phoenix.Common.IO;
using System.Net;

namespace Phoenix.Unity.PGL
{
    public enum InitFailureType
    {
        CRITICAL_ERROR,
        INVALID_ARGUMENTS,
        MISSING_DRM,
        INVALID_DRM,
        INVALID_DRM_ACTIVATION_ARGUMENTS,
        CORE_DOWNLOAD_FAILURE,
        MOD_LOAD_FAILURE,
        OFFLINE
    }

    /// <summary>
    /// Phoenix Game Launcher Support Class - Implementation helper for the Phoenix client runtime
    /// </summary>
    public static class PhoenixPGL
    {
        /// <summary>
        /// Initialization event handler
        /// </summary>
        public delegate void InitializeEventHandler();

        /// <summary>
        /// Event handler for when the connection to the internet and/or Phoenix servers is lost, also run if the token fails to refresh
        /// </summary>
        public delegate void ConnectionLossEventHandler();

        /// <summary>
        /// Initialization failure event handler
        /// </summary>
        /// <param name="failureType">Failure type</param>
        public delegate void InitializeFailureEventHandler(InitFailureType failureType);

        /// <summary>
        /// Called during setup, after the basic bindings are made, before the game descriptor is loaded
        /// </summary>
        public static event InitializeEventHandler OnInit;

        /// <summary>
        /// Called when the connection to the internet and/or Phoenix servers is lost, also run if the token fails to refresh
        /// </summary>
        public static event ConnectionLossEventHandler OnConnectionLoss;

        /// <summary>
        /// Called when setup fails (highly recommended to use)
        /// </summary>
        public static event InitializeFailureEventHandler OnSetupFailure;

        /// <summary>
        /// Phoenix API server
        /// </summary>
        public static string API = null;

        private static bool _setup;
        private static Logger _logger;
        private static List<Action> tickHandlers = new List<Action>();

        private static byte[] pglSecKey;
        private static byte[] pglSecIV;

        /// <summary>
        /// Checks if toplevel content security is enabled
        /// </summary>
        public static bool HasTopLevelDataSecurity
        {
            get
            {
                return pglSecKey != null;
            }
        }

        /// <summary>
        /// Retrieves the toplevel content security encryption key
        /// </summary>
        public static byte[] TopLevelDataKey
        {
            get
            {
                return pglSecKey;
            }
        }

        /// <summary>
        /// Retrieves the toplevel content security encryption IV
        /// </summary>
        public static byte[] TopLevelDataIV
        {
            get
            {
                return pglSecIV;
            }
        }

        /// <summary>
        /// Command line arguments
        /// </summary>
        public static Dictionary<string, string> CommandLineArguments = new Dictionary<string, string>();

        /// <summary>
        /// Sets up the runtime environment (requires to be run from Unity, not on a different thread)
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public static bool Setup()
        {
            if (_setup)
                return true;

            // Set up bindings
            CoreBindings.BindAll();

            // Set log level
            if (Application.isEditor || Debug.isDebugBuild)
                Logger.GlobalLogLevel = LogLevel.TRACE;

            // Create logger
            _logger = Logger.GetLogger("PGL");

            // Parse arguments
            _logger.Info("Parsing arguments...");
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("--"))
                {
                    string argS = arg.Substring(2);
                    if (argS.Contains("="))
                    {
                        string key = argS.Remove(argS.IndexOf("="));
                        string value = argS.Substring(argS.IndexOf("=") + 1);
                        arguments[key] = value;
                        _logger.Trace("  " + key + " = " + value);
                    }
                    else
                    {
                        if (argS == "activate")
                        {
                            arguments["activate"] = "true";
                            continue;
                        }
                        if (argS == "deactivate")
                        {
                            arguments["deactivate"] = "true";
                            continue;
                        }
                        if (i + 1 < args.Length)
                        {
                            arguments[argS] = args[i + 1];
                            i++;
                        }
                        else
                        {
                            _logger.Fatal("Invalid argument: " + arg + ": missing value.");
                            OnSetupFailure?.Invoke(InitFailureType.INVALID_ARGUMENTS);
                            return false;
                        }
                    }
                }
            }

            // Set log level
            if (arguments.ContainsKey("loglevel"))
            {
                switch (arguments["loglevel"])
                {
                    case "quiet":
                        Logger.GlobalLogLevel = LogLevel.QUIET;
                        break;
                    case "fatal":
                        Logger.GlobalLogLevel = LogLevel.FATAL;
                        break;
                    case "error":
                        Logger.GlobalLogLevel = LogLevel.ERROR;
                        break;
                    case "warn":
                        Logger.GlobalLogLevel = LogLevel.WARN;
                        break;
                    case "info":
                        Logger.GlobalLogLevel = LogLevel.INFO;
                        break;
                    case "trace":
                        if (Application.isEditor || Debug.isDebugBuild)
                            Logger.GlobalLogLevel = LogLevel.TRACE;
                        else
                        {
                            _logger.Fatal("Invalid argument: --loglevel: invalid value.");
                            OnSetupFailure?.Invoke(InitFailureType.INVALID_ARGUMENTS);
                            return false;
                        }
                        break;
                    case "debug":
                        if (Application.isEditor || Debug.isDebugBuild)
                            Logger.GlobalLogLevel = LogLevel.DEBUG;
                        else
                        {
                            _logger.Fatal("Invalid argument: --loglevel: invalid value.");
                            OnSetupFailure?.Invoke(InitFailureType.INVALID_ARGUMENTS);
                            return false;
                        }
                        break;
                    default:
                        _logger.Fatal("Invalid argument: --loglevel: invalid value.");
                        OnSetupFailure?.Invoke(InitFailureType.INVALID_ARGUMENTS);
                        return false;
                }
            }

            // Call init event
            CommandLineArguments = arguments;
            OnInit?.Invoke();

            // Set up ticker
            _logger.Info("Setting up essentials...");
            GameObject tickObj = new GameObject();
            tickObj.name = "~PhoenixPGL";
            GameObject.DontDestroyOnLoad(tickObj);
            tickObj.AddComponent<PGL_TickUtil>();

            // Read local game descriptor file
            _logger.Info("Loading game descriptor...");
            string descriptor;
            try
            {
                descriptor = AssetManager.GetAssetString("game.info").Replace("\r", "");
            }
            catch
            {
                _logger.Fatal("Missing game descriptor! Please add an asset named game.info in Assets/Resources/PhoenixAssets!");
                OnSetupFailure?.Invoke(InitFailureType.CRITICAL_ERROR);
                _logger.Fatal("Unable to continue, exiting game...");
                Application.Quit(1);
                return false;
            }

            // Parse descriptor
            _logger.Trace("Parsing game descriptor...");
            Dictionary<string, string> game = new Dictionary<string, string>();
            foreach (string line in descriptor.Split('\n'))
            {
                if (line == "" || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                // Parse
                if (line.Contains(": "))
                {
                    string key = line.Remove(line.IndexOf(": "));
                    string value = line.Substring(line.IndexOf(": ") + 2);
                    game[key] = value;
                    _logger.Trace("  " + key + " = " + value);
                }
                else
                {
                    _logger.Fatal("Corrupted game descriptor! Please modify game.info and correct the syntax!");
                    OnSetupFailure?.Invoke(InitFailureType.CRITICAL_ERROR);
                    _logger.Fatal("Unable to continue, exiting game...");
                    Application.Quit(1);
                    return false;
                }
            }
            _logger.Trace("Verifying game descriptor...");
            if (!game.ContainsKey("Game-ID") || !game.ContainsKey("Game-Title"))
            {
                _logger.Fatal("Invalid game descriptor! Please modify game.info and include at least the following fields: Game-ID, Game-Title.");
                OnSetupFailure?.Invoke(InitFailureType.CRITICAL_ERROR);
                _logger.Fatal("Unable to continue, exiting game...");
                Application.Quit(1);
                return false;
            }
            bool hasVersion = game.ContainsKey("Game-Version");
            if (!game.ContainsKey("Game-Version"))
            {
#if UNITY_EDITOR
                game["Game-Version"] = "DEVELOPMENT";
#endif
#if !UNITY_EDITOR
                game["Game-Version"] = "PROD-DEFAULT";
#endif
            }
            if (!game.ContainsKey("Game-Channel"))
            {
#if UNITY_EDITOR
                game["Game-Channel"] = "DEVELOPMENT";
#endif
#if !UNITY_EDITOR
                game["Game-Channel"] = "PROD-DEFAULT";
#endif
            }
            if (!game.ContainsKey("Asset-Identifier"))
            {
#if UNITY_EDITOR
                game["Asset-Identifier"] = game["Game-ID"] + "/devbuild";
#endif
#if !UNITY_EDITOR
                game["Asset-Identifier"] = game["Game-ID"] + "/proddefault";
#endif
            }
            if (!game.ContainsKey("Offline-Support"))
                game["Offline-Support"] = "False";
            if (!game.ContainsKey("Mod-Support"))
                game["Mod-Support"] = "False";
            game["Session"] = "OFFLINE";

#if !UNITY_EDITOR
            // Production client, we need to load the game document

            // Check arguments
            _logger.Trace("Verifying arguments...");
            if (arguments.ContainsKey("activate") && !arguments.ContainsKey("productkey"))
            {
                _logger.Fatal("Missing 'productkey' argument, unable to activate product.");
                OnSetupFailure?.Invoke(InitFailureType.INVALID_DRM_ACTIVATION_ARGUMENTS);
                return false;
            }

            // Prepare folders
            _logger.Trace("Preparing folders...");
            string phoenixRoot = Application.persistentDataPath;
            Directory.CreateDirectory(phoenixRoot);
            Directory.CreateDirectory(phoenixRoot + "/corefiles");

            // Prepare symmetric encryption layer via playerprefs
            _logger.Trace("Processing data...");
            bool regenerateCoreRsa = false;
            RSACryptoServiceProvider provRsa = null;
            if (!PlayerPrefs.HasKey("phoenix-core-sec-key"))
                regenerateCoreRsa = true;
            else
            {
                try
                {
                    // Load
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
                    rsa.ImportCspBlob(Convert.FromBase64String(PlayerPrefs.GetString("phoenix-core-sec-key")));
                    provRsa = rsa;
                }
                catch
                {
                    regenerateCoreRsa = true;
                }
            }
            if (regenerateCoreRsa)
            {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
                byte[] keyD = rsa.ExportCspBlob(true);
                PlayerPrefs.SetString("phoenix-core-sec-key", Convert.ToBase64String(keyD));
                provRsa = rsa;
            }

            // Prepare key
            bool regenerateKey = false;
            string keyFile = phoenixRoot + "/corefiles/" + Sha512Hash("sec-" + game["Game-ID"] + ".pdk") + ".pa";
            if (!File.Exists(keyFile))
                regenerateKey = true;
            else
            {
                // Load
                try
                {
                    // Open file
                    MemoryStream fI = new MemoryStream(provRsa.Decrypt(File.ReadAllBytes(keyFile), RSAEncryptionPadding.Pkcs1));

                    // Read data
                    DataReader rd = new DataReader(fI);
                    pglSecKey = rd.ReadBytes();
                    pglSecIV = rd.ReadBytes();

                    // Close
                    fI.Close();
                }
                catch
                {
                    regenerateKey = true;
                }
            }

            // Regenerate if needed
            if (regenerateKey)
            {
                // Generate
                using (Aes aes = Aes.Create())
                {
                    pglSecKey = aes.Key;
                    pglSecIV = aes.IV;
                }

                // Open output
                MemoryStream fO = new MemoryStream();

                // Write data
                DataWriter wr = new DataWriter(fO);
                wr.WriteBytes(pglSecKey);
                wr.WriteBytes(pglSecIV);

                // Save
                File.WriteAllBytes(keyFile, provRsa.Encrypt(fO.ToArray(), RSAEncryptionPadding.Pkcs1));

                // Close
                fO.Close();
            }

            // Activate if needed
            string drmDataPath = phoenixRoot + "/corefiles/" + Sha512Hash("drm-" + game["Game-ID"] + ".pdi") + ".pa";
            if (arguments.ContainsKey("activate"))
            {
                // Activate
                string product = arguments["productkey"];
                _logger.Info("Activating product...");
                try
                {
                    // Contact Phoenix
                    HttpClient cl = new HttpClient();
                    cl.DefaultRequestHeaders.Add("With-Phoenix-DRM", "true");
                    cl.DefaultRequestHeaders.Add("With-Phoenix-Product-Key", Sha512Hash(product.ToUpper().Replace("-", "")));

                    // Build URL
                    string url;
                    if (API == null)
                        url = PhoenixEnvironment.DefaultAPIServer;
                    else
                        url = API;
                    if (!url.EndsWith("/"))
                        url += "/";

                    // Send request
                    var r = cl.GetAsync(url + "data/files/" + game["Game-ID"] + "/" + game["Game-ID"] + (hasVersion ? "-" + game["Game-Version"] : "") + ".game").GetAwaiter().GetResult();

                    // Handle errors
                    if (r.StatusCode == HttpStatusCode.Forbidden)
                    {
                        if (r.ReasonPhrase == "Bad DRM key")
                        {
                            // Invalid DRM
                            _logger.Fatal("Failed to authenticate the game! Product key was invalid!");
                            Application.Quit(1);
                            return false;
                        }
                    }
                    string res = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (res == null || res == "")
                        throw new Exception();

                    // Save
                    _logger.Info("Saving DRM information...");

                    // Write encrypted
                    Stream fO = File.OpenWrite(drmDataPath);
                    EncryptTransfer(new MemoryStream(Encoding.UTF8.GetBytes(Sha512Hash(product.ToUpper().Replace("-", "")))), fO);
                    fO.Close();

                    // DRM verified
                    _logger.Info("Product activated successfully!");
                }
                catch
                {
                    _logger.Fatal("Failed to authenticate the game! Please verify the connection with the server and the product key!");
                    Application.Quit(1);
                    return false;
                }
            }
            else if (arguments.ContainsKey("deactivate"))
            {
                if (File.Exists(drmDataPath))
                {
                    _logger.Info("Deactivating product...");
                    File.Delete(drmDataPath);
                    _logger.Info("DRM data wiped!");
                }
            }

            // Perform game authentication
            _logger.Info("Authenticating game...");
            string localDocPath = phoenixRoot + "/corefiles/" + Sha512Hash("gd-" + game["Game-ID"] + "-" + game["Game-Version"] + "-" + game["Game-Channel"] + ".pgd") + ".pa";
            try
            {
                // Contact Phoenix
                HttpClient cl = new HttpClient();
                cl.DefaultRequestHeaders.Add("With-Phoenix-DRM", File.Exists(drmDataPath) ? "true" : "false");

                // Add DRM headers
                if (File.Exists(drmDataPath))
                {
                    // Load key
                    Stream kIn = File.OpenRead(drmDataPath);
                    MemoryStream kO = new MemoryStream();
                    DecryptTransfer(kIn, kO);
                    kIn.Close();
                    string keyStr = Encoding.UTF8.GetString(kO.ToArray());
                    cl.DefaultRequestHeaders.Add("With-Phoenix-Product-Key", keyStr);
                }

                // Build url
                string url;
                if (API == null)
                    url = PhoenixEnvironment.DefaultAPIServer;
                else
                    url = API;
                if (!url.EndsWith("/"))
                    url += "/";

                // Send request
                var r = cl.GetAsync(url + "data/files/" + game["Game-ID"] + "/" + game["Game-ID"] + (hasVersion ? "-" + game["Game-Version"] : "") + ".game").GetAwaiter().GetResult();

                // Handle errors
                if (r.StatusCode == HttpStatusCode.Forbidden)
                {
                    if (r.ReasonPhrase == "Bad DRM key")
                    {
                        // Invalid DRM
                        _logger.Fatal("Failed to authenticate the game! DRM invalid!");
                        OnSetupFailure?.Invoke(InitFailureType.INVALID_DRM);
                        return false;
                    }
                    else if (r.ReasonPhrase == "Missing DRM")
                    {
                        // Invalid DRM
                        _logger.Fatal("Failed to authenticate the game! Missing DRM!");
                        OnSetupFailure?.Invoke(InitFailureType.MISSING_DRM);
                        return false;
                    }
                }
                string res = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (res == null || res == "")
                    throw new Exception();

                // Handle response
                string successMsg = "Game authenticated:";
                foreach (string line in res.Split('\n'))
                {
                    if (line == "")
                        continue;
                    string key = line.Remove(line.IndexOf(": "));
                    string value = line.Substring(line.IndexOf(": ") + 2);
                    game[key] = value;
                    successMsg += "\n  " + key + ": " + value;
                }
                _logger.Info(successMsg);
            }
            catch
            {
                // Authentication failure

                // Check existing game descriptor file
                if (!File.Exists(localDocPath))
                {
                    // Error
                    _logger.Fatal("Failed to authenticate the game! Please verify the connection with the server!");
                    OnSetupFailure?.Invoke(InitFailureType.CORE_DOWNLOAD_FAILURE);
                    return false;
                }
                else
                {
                    try
                    {
                        // Load offline document
                        _logger.Info("Failed to contact game servers, loading locally-stored data...");
                        MemoryStream lPgdO = new MemoryStream();
                        Stream lPgdI = File.OpenRead(localDocPath);
                        DecryptTransfer(lPgdI, lPgdO);
                        lPgdI.Close();

                        // Load data from document
                        string successMsg = "Game data loaded:";
                        lPgdO = new MemoryStream(lPgdO.ToArray());
                        DataReader rd = new DataReader(lPgdO);
                        int l = rd.ReadInt();
                        for (int i = 0; i < l; i++)
                        {
                            string key = rd.ReadString();
                            string val = rd.ReadString();

                            // Add
                            game[key] = val;
                            successMsg += "\n  " + key + ": " + val;
                        }
                        _logger.Info(successMsg);
                    }
                    catch
                    {
                        // Error
                        _logger.Fatal("Failed to authenticate the game and local data was not available! Please verify the connection with the server!");
                        OnSetupFailure?.Invoke(InitFailureType.CORE_DOWNLOAD_FAILURE);
                        return false;
                    }
                }
            }

            // Build offline doc
            Dictionary<string, string> gameInfoCurrent = new Dictionary<string, string>();
            foreach (string key in game.Keys)
                if (key != "Session" && key != "Product-Key")
                    gameInfoCurrent[key] = game[key];
            gameInfoCurrent["Session"] = "OFFLINE";

            // Save doc
            _logger.Info("Saving game information for offline play...");
            MemoryStream fPgdI = new MemoryStream();
            DataWriter pgdO = new DataWriter(fPgdI);
            pgdO.WriteInt(gameInfoCurrent.Count);
            foreach (string key in gameInfoCurrent.Keys)
            {
                pgdO.WriteString(key);
                pgdO.WriteString(gameInfoCurrent[key]);
            }

            // Write encrypted
            Stream fPgdO = File.OpenWrite(localDocPath);
            EncryptTransfer(new MemoryStream(fPgdI.ToArray()), fPgdO);
            fPgdO.Close();

            // Parse root
            string gameRoot = Application.dataPath;
            if (Application.platform != RuntimePlatform.Android) 
                gameRoot = Path.GetDirectoryName(gameRoot);

            // Set up environment
            _logger.Info("Setting up Phoenix environment...");
            game["Assets-Path"] = phoenixRoot + "/assets/" + game["Game-Channel"] + "/" + game["Game-Version"] + "/" + game["Asset-Identifier"];
            game["Game-Storage-Path"] = gameRoot;
            game["Player-Data-Path"] = phoenixRoot + "/playerdata/" + game["Game-ID"];
            game["Save-Data-Path"] = phoenixRoot + "/savedata/" + game["Game-ID"];
            if (!game.ContainsKey("Refresh-Endpoint")) {
                string urlA;
                if (API == null)
                    urlA = PhoenixEnvironment.DefaultAPIServer;
                else
                    urlA = API;
                if (!urlA.EndsWith("/"))
                    urlA += "/";
                game["Refresh-Endpoint"] = urlA + "tokens/refresh";
            }

            // If DRM was present, use the product hash for other core assets
            if (File.Exists(drmDataPath))
            {
                // Load key
                Stream kIn = File.OpenRead(drmDataPath);
                MemoryStream kO = new MemoryStream();
                DecryptTransfer(kIn, kO);
                kIn.Close();
                string keyStr = Encoding.UTF8.GetString(kO.ToArray());
                
                // Load new key
                regenerateKey = false;
                string assetEncryptionKeyFile = phoenixRoot + "/corefiles/" + Sha512Hash("sec-assets-" + game["Game-ID"] + ".pdk") + ".pa";
                if (!File.Exists(assetEncryptionKeyFile))
                    regenerateKey = true;
                else
                {
                    // Load
                    try
                    {
                        // Open file
                        MemoryStream fI = new MemoryStream(provRsa.Decrypt(File.ReadAllBytes(assetEncryptionKeyFile), RSAEncryptionPadding.Pkcs1));

                        // Read data
                        DataReader rd = new DataReader(fI);
                        pglSecKey = rd.ReadBytes();
                        pglSecIV = rd.ReadBytes();

                        // Close
                        fI.Close();
                    }
                    catch
                    {
                        regenerateKey = true;
                    }
                }

                // Regenerate if needed
                if (regenerateKey)
                {
                    // Generate
                    using (Aes aes = Aes.Create())
                    {
                        pglSecKey = aes.Key;
                        pglSecIV = aes.IV;
                    }

                    // Open output
                    MemoryStream fO = new MemoryStream();

                    // Write data
                    DataWriter wr = new DataWriter(fO);
                    wr.WriteBytes(pglSecKey);
                    wr.WriteBytes(pglSecIV);

                    // Save
                    File.WriteAllBytes(assetEncryptionKeyFile, provRsa.Encrypt(fO.ToArray(), RSAEncryptionPadding.Pkcs1));

                    // Close
                    fO.Close();
                }
            }
#endif
#if UNITY_EDITOR
            string runPath = "Run";

            // Editor mode, check for a editor configuration
            if (!File.Exists("Assets/Editor/phoenixdebug.txt"))
            {
                // No debug configuration
                _logger.Warn("No Phoenix debug configuration found!");
                _logger.Warn("Starting without product information...");
                _logger.Warn("Please create the file 'Assets/Editor/phoenixdebug.txt' to configure the debug environment.");
                _logger.Warn("Example configuration content:\n" +
                            "  Product-Key: 12345-ABCDE-67890-FGHIJ\n" +
                            "  Debug-Folder: Run\n" +
                            "  Log-Level: TRACE");
                game["Offline-Support"] = "True";
            }
            else
            {
                _logger.Info("Loading debug configuration...");
                Dictionary<string, string> debugConfig = new Dictionary<string, string>();
                string conf = File.ReadAllText("Assets/Editor/phoenixdebug.txt").Replace("\r", "");
                foreach (string line in conf.Split('\n'))
                {
                    if (line == "" || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    // Parse
                    if (line.Contains(": "))
                    {
                        string key = line.Remove(line.IndexOf(": "));
                        string value = line.Substring(line.IndexOf(": ") + 2);
                        debugConfig[key] = value;
                    }
                }

                // Log level
                if (debugConfig.ContainsKey("Log-Level"))
                {
                    switch (debugConfig["Log-Level"].ToLower())
                    {
                        case "quiet":
                            Logger.GlobalLogLevel = LogLevel.QUIET;
                            break;
                        case "fatal":
                            Logger.GlobalLogLevel = LogLevel.FATAL;
                            break;
                        case "error":
                            Logger.GlobalLogLevel = LogLevel.ERROR;
                            break;
                        case "warn":
                            Logger.GlobalLogLevel = LogLevel.WARN;
                            break;
                        case "info":
                            Logger.GlobalLogLevel = LogLevel.INFO;
                            break;
                        case "trace":
                            Logger.GlobalLogLevel = LogLevel.TRACE;
                            break;
                        case "debug":
                            Logger.GlobalLogLevel = LogLevel.DEBUG;
                            break;
                    }
                    _logger.Info("Set log level to " + Logger.GlobalLogLevel);
                }

                // Debug folder
                if (debugConfig.ContainsKey("Debug-Folder"))
                {
                    runPath = debugConfig["Debug-Folder"];
                    Directory.CreateDirectory(runPath);
                    _logger.Info("Set debug folder to " + Path.GetFullPath(runPath));
                }

                // Product information
                if (debugConfig.ContainsKey("Product-Key"))
                {
                    _logger.Info("Authenticating game...");
                    game["Product-Key"] = debugConfig["Product-Key"];
                    
                    try
                    {
                        // Contact Phoenix
                        HttpClient cl = new HttpClient();
                        cl.DefaultRequestHeaders.Add("With-Phoenix-DRM", "true");
                        cl.DefaultRequestHeaders.Add("With-Phoenix-Product-Key", Sha512Hash(game["Product-Key"].ToUpper().Replace("-", "")));

                        // Build url
                        string url;
                        if (API == null)
                            url = PhoenixEnvironment.DefaultAPIServer;
                        else
                            url = API;
                        if (!url.EndsWith("/"))
                            url += "/";
                        string res = cl.GetAsync(url + "data/files/" + game["Game-ID"] + "/" + game["Game-ID"] + (hasVersion ? "-" + game["Game-Version"] : "") + ".game").GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (res == null || res == "")
                            throw new Exception();
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        string successMsg = "Game authenticated:";
                        foreach (string line in res.Split('\n'))
                        {
                            if (line == "")
                                continue;
                            string key = line.Remove(line.IndexOf(": "));
                            string value = line.Substring(line.IndexOf(": ") + 2);
                            data[key] = value;
                            successMsg += "\n  " + key + ": " + value;
                        }
                        _logger.Info(successMsg);
                        game["Session"] = data["Session"];
                    }
                    catch
                    {
                        _logger.Fatal("Failed to authenticate the game! Please verify the connection with the server and the product key!");
                        Application.Quit(1);
                        return false;
                    }
                }
            }

            // Parse root
            string gameRoot = Application.dataPath;
            if (Application.platform != RuntimePlatform.Android) 
                gameRoot = Path.GetDirectoryName(gameRoot);

            // Set fields
            Directory.CreateDirectory(runPath);
            _logger.Info("Setting up debug environment fields...");
            game["Assets-Path"] = runPath + "/assets/" + game["Game-Channel"] + "/" + game["Game-Version"] + "/" + game["Asset-Identifier"];
            game["Game-Storage-Path"] = gameRoot;
            game["Player-Data-Path"] = runPath + "/playerdata";
            game["Save-Data-Path"] = runPath + "/savedata";

            string urlA;
            if (API == null)
                urlA = PhoenixEnvironment.DefaultAPIServer;
            else
                urlA = API;
            if (!urlA.EndsWith("/"))
                urlA += "/";
            game["Refresh-Endpoint"] = urlA + "tokens/refresh";
#endif

            // Log final game descriptor
            string msg = "Final game descriptor:";
            foreach (string key in game.Keys)
                if (key != "Session" && key != "Product-Key")
                    msg += "\n  " + key + " = " + game[key];
            _logger.Trace(msg);

            // Prepare directories
            _logger.Info("Creating directories...");
            Directory.CreateDirectory(game["Assets-Path"]);
            Directory.CreateDirectory(game["Game-Storage-Path"]);
            Directory.CreateDirectory(game["Player-Data-Path"]);
            Directory.CreateDirectory(game["Save-Data-Path"]);

            // Set up game environment
            _logger.Info("Setting up environment...");
            GameImpl impl = new GameImpl();
            impl.Register(game);

            // Set API of login manager
            LoginManager.API = API;

            // Contact Phoenix to test network connection by refreshing the token
            if (!Game.IsOffline)
            {
                try
                {
                    // Contact Phoenix
                    HttpClient cl = new HttpClient();
                    cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + Game.SessionToken);
                    string res = cl.GetAsync(game["Refresh-Endpoint"]).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (res != null && res != "")
                        impl.RefreshToken(res.Trim()); // Success
                    else
                        impl.RefreshFailure(); // Failed
                }
                catch
                {
                    // Failure
                    impl.RefreshFailure();
                }
            }

            // Mod support
            if (game["Mod-Support"] == "True")
            {
                // Prepare
                _logger.Info("Loading mods...");
                ModManager manager = new ModManager();

                // Check if the platform can run mods
                bool platformSupportsMods = false;
                switch (Application.platform)
                {
                    case UnityEngine.RuntimePlatform.OSXEditor:
                    case UnityEngine.RuntimePlatform.OSXPlayer:
                    case UnityEngine.RuntimePlatform.WindowsPlayer:
                    case UnityEngine.RuntimePlatform.WindowsEditor:
                    case UnityEngine.RuntimePlatform.Android:
                    case UnityEngine.RuntimePlatform.LinuxPlayer:
                    case UnityEngine.RuntimePlatform.LinuxEditor:
                    case UnityEngine.RuntimePlatform.LinuxServer:
                    case UnityEngine.RuntimePlatform.WindowsServer:
                    case UnityEngine.RuntimePlatform.OSXServer:
                        platformSupportsMods = true;
                        break;
                }

                // Enable support if posssible
                if (platformSupportsMods)
                {
                    // Check integrated server support
                    try
                    {
                        // Check support
                        bool hasServerAssemblies = false;
                        bool hasIntegratedServerAssemblies = false;
                        Type bindingsType = null;
                        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                if (asm.GetType("Phoenix.Server.GameServer") != null)
                                    hasServerAssemblies = true;
                                if (asm.GetType("Phoenix.Client.IntegratedServerBootstrapper.PhoenixIntegratedServer") != null)
                                    hasIntegratedServerAssemblies = true;
                            }
                            catch
                            {
                            }
                        }
                        if (hasServerAssemblies && hasIntegratedServerAssemblies)
                        {
                            // Load bindings
                            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                try
                                {
                                    bindingsType = asm.GetType("Phoenix.Unity.PGL.Mods.IntegratedServerSupport.ModIntegratedServerSupportBindings");
                                    if (bindingsType != null)
                                        break;
                                }
                                catch
                                {
                                }
                            }

                            // Bind
                            if (bindingsType != null)
                                bindingsType.GetMethod("Bind", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { manager });
                        }
                    }
                    catch
                    {
                        // No support
                    }

                    // Load mods
                    PGL_TickUtil.cleanupAction = () => manager.Unload();
                    Directory.CreateDirectory(Game.SaveData + "/Mods");
                    foreach (FileInfo mod in new DirectoryInfo(Game.SaveData + "/Mods").GetFiles("*.pmbp"))
                    {
                        _logger.Info("Loading mod package: " + mod.Name + "...");
                        Directory.CreateDirectory(Game.SaveData + "/Mods/" + Path.GetFileNameWithoutExtension(mod.Name));
                        try
                        {
                            // Load mod
                            FileStream strm = mod.OpenRead();
                            ModInfo info = manager.Load(strm, Path.GetFullPath(Game.SaveData + "/Mods/" + Path.GetFileNameWithoutExtension(mod.Name)));

                            // Add tick handler
                            tickHandlers.Add(() => info.Instance.Tick());
                        }
                        catch (Exception e)
                        {
                            _logger.Fatal("Mod loading failed: " + mod.Name, e);
                            OnSetupFailure?.Invoke(InitFailureType.MOD_LOAD_FAILURE);
                            return false;
                        }
                    }
                    manager.LoadFinish();

                    // Bind game client creation
                    GameClientFactory.OnCreateClient += (client) =>
                    {
                        // Load components
                        _logger.Debug("Adding mod components to client...");
                        foreach (ModInfo mod in manager.GetMods())
                        {
                            _logger.Info("Loading mod components from " + mod.ID + "...");
                            Type[] types = mod.Assembly.GetTypes();
                            foreach (Type t in types)
                            {
                                if (t.GetCustomAttribute<Client.ModComponent>() != null)
                                {
                                    _logger.Debug("Loading component type: " + t.Name + "...");
                                    if (!typeof(Component).IsAssignableFrom(t) && !typeof(IComponentPackage).IsAssignableFrom(t))
                                    {
                                        _logger.Error("Could not load mod component: " + t.FullName + ", mod: " + mod.Package.Name + ": not a client component or package!");
                                        continue;
                                    }
                                    ConstructorInfo constr = t.GetConstructor(new Type[0]);
                                    if (constr == null)
                                    {
                                        _logger.Error("Could not load mod component: " + t.FullName + ", mod: " + mod.Package.Name + ": no constructor that takes 0 arguments!");
                                        continue;
                                    }
                                    if (typeof(Component).IsAssignableFrom(t))
                                    {
                                        Component comp = (Component)constr.Invoke(new object[0]);

                                        // Add to client
                                        client.AddComponent(comp);
                                    }
                                    else
                                    {
                                        IComponentPackage comp = (IComponentPackage)constr.Invoke(new object[0]);

                                        // Add to client
                                        client.AddComponentPackage(comp);
                                    }
                                }
                            }
                        }
                    };
                }
            }

            // Lock asset manager
            AssetManager.Lock();

            // Log completion
            if (Game.IsOffline)
            {
                _logger.Info(Game.Title + ", version " + Game.Version
                    + "\n  Game ID: " + Game.GameID
                    + "\n  Game Title: " + Game.Title
                    + "\n  Game Version: " + Game.Version
                    + "\n  Game Data: " + Game.GameFiles
                    + "\n  Asset Data: " + Game.AssetsFolder
                    + "\n  Save Data: " + Game.SaveData
                    + "\n  Player Data: " + Game.PlayerData
                    + "\n  Network status: offline.");
                if (!Game.OfflineSupport)
                {
                    _logger.Fatal("Unable to continue! " + Game.Title + " requires an active network connection in order to function!");
                    OnSetupFailure?.Invoke(InitFailureType.OFFLINE);
                    return false;
                }
            }
            else
                _logger.Info(Game.Title + ", version " + Game.Version
                    + "\n  Game ID: " + Game.GameID
                    + "\n  Game Title: " + Game.Title
                    + "\n  Game Version: " + Game.Version
                    + "\n  Game Data: " + Game.GameFiles
                    + "\n  Asset Data: " + Game.AssetsFolder
                    + "\n  Save Data: " + Game.SaveData
                    + "\n  Player Data: " + Game.PlayerData
                    + "\n  Network status: online.");
            _logger.Info("Successfully launched the game");

            // Set up token refresh and disconnect handlers
            if (!Game.IsOffline)
            {
                // Start refresher
                Thread th = new Thread(() =>
                {
                    int connectCheck = 30;
                    while (!Game.IsOffline)
                    {
                        connectCheck--;
                        if (connectCheck <= 0)
                        {
                            connectCheck = 30;

                            // Test connection with phoenix
                            try
                            {
                                HttpClient cl = new HttpClient();

                                string url;
                                if (API == null)
                                    url = PhoenixEnvironment.DefaultAPIServer;
                                else
                                    url = API;
                                if (!url.EndsWith("/"))
                                    url += "/";
                                if (!cl.GetAsync(url + "servers").GetAwaiter().GetResult().IsSuccessStatusCode)
                                    throw new Exception();
                            }
                            catch
                            {
                                _logger.Warn("Lost internet connection");
                                impl.RefreshFailure();
                                PGL_TickUtil.Schedule(() =>
                                {
                                    OnConnectionLoss?.Invoke();
                                });
                                break;
                            }
                        }

                        string tkn = Game.SessionToken;
                        if (!NetworkInterface.GetIsNetworkAvailable() || tkn == null)
                        {
                            _logger.Warn("Lost internet connection");
                            impl.RefreshFailure();
                            PGL_TickUtil.Schedule(() =>
                            {
                                OnConnectionLoss?.Invoke();
                            });
                            break;
                        }

                        // Parse token
                        string[] parts = tkn.Split('.');
                        string payloadJson = Encoding.UTF8.GetString(Base64Url.Decode(parts[1]));
                        JObject payload = JsonConvert.DeserializeObject<JObject>(payloadJson);
                        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (15 * 60) >= payload.GetValue("exp").ToObject<long>())
                        {
                            try
                            {
                                // Contact Phoenix
                                HttpClient cl = new HttpClient();
                                cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + Game.SessionToken);
                                string res = cl.GetAsync(game["Refresh-Endpoint"]).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                if (res != null && res != "")
                                    impl.RefreshToken(res.Trim()); // Success
                                else
                                    throw new Exception();
                            }
                            catch
                            {
                                _logger.Warn("Lost internet connection");
                                impl.RefreshFailure();
                                PGL_TickUtil.Schedule(() =>
                                {
                                    OnConnectionLoss?.Invoke();
                                });
                                break;
                            }
                        }
                        Thread.Sleep(1000);
                    }
                });
                th.IsBackground = true;
                th.Name = "Token Refresher";
                th.Start();
            }

            _setup = true;
            return true;
        }

        /// <summary>
        /// Called from unity to tick the client (INTERNAL)
        /// </summary>
        internal static void Tick()
        {
            // Tick attached game clients
            Phoenix.Client.GameClient.GlobalTick();

            // Tick mods and other handlers
            Action[] handlers;
            while (true)
            {
                try
                {
                    handlers = tickHandlers.ToArray();
                    break;
                }
                catch { }
            }
            foreach (Action ac in handlers)
            {
                if (ac == null)
                    tickHandlers.Remove(ac);
                try
                {
                    ac();
                }
                catch (Exception e)
                {
                    _logger.Error("Exception thrown during client tick", e);
                }
            }
        }

        private static void EncryptTransfer(Stream source, Stream output)
        {
            using (Aes aes = Aes.Create())
            {
                // Load key
                aes.Key = pglSecKey;
                aes.IV = pglSecIV;

                // Create stream
                CryptoStream strm = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
                source.CopyTo(strm);
                strm.Close();
            }
        }

        private static void DecryptTransfer(Stream source, Stream output)
        {
            using (Aes aes = Aes.Create())
            {
                // Load key
                aes.Key = pglSecKey;
                aes.IV = pglSecIV;

                // Create stream
                CryptoStream strm = new CryptoStream(source, aes.CreateDecryptor(), CryptoStreamMode.Read);
                strm.CopyTo(output);
                strm.Close();
            }
        }

        private static string Sha512Hash(string input)
        {
            return string.Concat(SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(input)).Select(x => x.ToString("x2")));
        }

        private static string Sha256Hash(string input)
        {
            return string.Concat(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input)).Select(x => x.ToString("x2")));
        }

        private static class Base64Url
        {
            public static string Encode(byte[] arg)
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("arg");
                }

                var s = Convert.ToBase64String(arg);
                return s
                    .Replace("=", "")
                    .Replace("/", "_")
                    .Replace("+", "-");
            }

            public static string ToBase64(string arg)
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("arg");
                }

                var s = arg
                        .PadRight(arg.Length + (4 - arg.Length % 4) % 4, '=')
                        .Replace("_", "/")
                        .Replace("-", "+");

                return s;
            }

            public static byte[] Decode(string arg)
            {
                return Convert.FromBase64String(ToBase64(arg));
            }
        }
    }
}
