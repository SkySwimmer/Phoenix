using Phoenix.Common.Logging;
using GameObject = UnityEngine.GameObject;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using Phoenix.Server;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Phoenix.Common;
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

namespace Phoenix.Unity.PGL
{
    public enum InitFailureType
    {
        CRITICAL_ERROR,
        INVALID_ARGUMENTS,
        MISSING_LAUNCHER_ARGUMENTS,
        LAUNCHER_DATA_PARSING_FAILURE,
        LAUNCHER_CONNECTION_FAILURE,
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
        public static string? API = null;

        private static bool _setup;
        private static Logger _logger;
        private static List<Action> tickHandlers = new List<Action>();

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
                        if (argS == "play")
                        {
                            arguments["play"] = "true";
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
            if (!game.ContainsKey("Game-Version"))
                game["Game-Version"] = "DEVELOPMENT";
            if (!game.ContainsKey("Game-Channel"))
                game["Game-Channel"] = "DEVELOPMENT";
            if (!game.ContainsKey("Asset-Identifier"))
                game["Asset-Identifier"] = game["Game-ID"] + "/devbuild";
            if (!game.ContainsKey("Offline-Support"))
                game["Offline-Support"] = "False";
            if (!game.ContainsKey("Mod-Support"))
                game["Mod-Support"] = "False";
            game["Session"] = "OFFLINE";

#if !UNITY_EDITOR
            // Production client, we need to load the game document

            // Check arguments
            _logger.Trace("Verifying arguments...");
            if (!arguments.ContainsKey("play") || !arguments.ContainsKey("gamedoc"))
            {
                _logger.Fatal("Unable to start with missing arguments, please use the launcher.");
                OnSetupFailure?.Invoke(InitFailureType.MISSING_LAUNCHER_ARGUMENTS);
                return false;
            }

            // Contact launcher
            _logger.Info("Contacting launcher...");

            try
            {
                // Download
                TcpClient cl = new TcpClient("127.0.0.1", int.Parse(arguments["gamedoc"]));

                string str = cl.Client.RemoteEndPoint.ToString();
                _logger.Trace("Connected to " + str);
                _logger.Trace("Downloading game descriptor...");
                MemoryStream strm = new MemoryStream();
                while (true)
                {
                    try
                    {
                        int i = cl.GetStream().ReadByte();
                        if (i == -1)
                            break;
                        strm.WriteByte((byte)i);
                    }
                    catch
                    {
                        break;
                    }
                }
                try
                {
                    cl.Close();
                }
                catch
                {
                }
                _logger.Trace("Disconnected from " + str);
                byte[] data = strm.ToArray();

                try
                {
                    _logger.Trace("Processing game descriptor...");

                    // Decode
                    string gameData = Encoding.UTF8.GetString(data);
                    foreach (string line in gameData.Split("\n"))
                    {
                        if (line == "")
                            continue;
                        string key = line.Remove(line.IndexOf(": "));
                        string value = line.Substring(line.IndexOf(": ") + 2);
                        if (key != "Game-ID" && key != "Mod-Support")
                            game[key] = value;
                        else if (key == "Game-ID")
                        {
                            if (value != game["Game-ID"])
                            {
                                _logger.Fatal("Failed to communicate with the launcher: launcher sent data related to a different game.");
                                OnSetupFailure?.Invoke(InitFailureType.LAUNCHER_DATA_PARSING_FAILURE);
                                return false;
                            }
                        }
                    }
                }
                catch
                {
                    _logger.Fatal("Failed to communicate with the launcher: launcher sent invalid data.");
                    OnSetupFailure?.Invoke(InitFailureType.LAUNCHER_DATA_PARSING_FAILURE);
                    return false;
                }
            }
            catch
            {
                _logger.Fatal("Failed to communicate with the launcher.");
                OnSetupFailure?.Invoke(InitFailureType.LAUNCHER_CONNECTION_FAILURE);
                return false;
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
                            "  Digital-Seal: W2dhbWVpZDp0ZXN0LHtz....\n" +
                            "  Debug-Folder: Run\n" +
                            "  Log-Level: TRACE");
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
                if (debugConfig.ContainsKey("Product-Key") && debugConfig.ContainsKey("Digital-Seal"))
                {
                    _logger.Info("Authenticating game...");
                    game["Product-Key"] = debugConfig["Product-Key"];
                    game["Digital-Seal"] = debugConfig["Digital-Seal"];
                    try
                    {
                        // Decode seal
                        JObject[] seal = JsonConvert.DeserializeObject<JObject[]>(Encoding.UTF8.GetString(Base64Url.Decode(game["Digital-Seal"])));
                        JObject payload = seal[0];
                        string productHash = payload.GetValue("producthash").ToObject<string>();
                        long timestamp = payload.GetValue("timestamp").ToObject<long>();

                        // Contact Phoenix
                        HttpClient cl = new HttpClient();
                        cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + Game.SessionToken);
                        cl.DefaultRequestHeaders.Add("Product-Key", game["Product-Key"]);
                        cl.DefaultRequestHeaders.Add("Digital-Seal", game["Digital-Seal"]);

                        string url;
                        if (API == null)
                            url = PhoenixEnvironment.DefaultAPIServer;
                        else
                            url = API;
                        if (!url.EndsWith("/"))
                            url += "/";
                        string res = cl.GetAsync(url + "data/files/" + game["Game-ID"] + "/" + productHash + "/" + timestamp + "/" + game["Game-ID"] + ".game").GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (res == null || res == "")
                            throw new Exception();
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        string successMsg = "Game authenticated:";
                        foreach (string line in res.Split("\n"))
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
                        _logger.Fatal("Failed to authenticate the game!");
                        Application.Quit(1);
                        return false;
                    }
                }
            }

            // Set fields
            Directory.CreateDirectory(runPath);
            _logger.Info("Setting up debug environment fields...");
            game["Assets-Path"] = runPath + "/assets/" + game["Game-Channel"] + "/" + game["Game-Version"] + "/" + game["Asset-Identifier"];
            game["Game-Storage-Path"] = runPath + "/gamefiles";
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
                if (key != "Session" && key != "Product-Key" && key != "Digital-Seal")
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
                _logger.Info("Loading mods...");
                ModManager manager = new ModManager();
                PGL_TickUtil.cleanupAction = () => manager.Unload();
                Directory.CreateDirectory(Game.GameFiles + "/Mods");
                foreach (FileInfo mod in new DirectoryInfo(Game.GameFiles + "/Mods").GetFiles("*.pmbp"))
                {
                    _logger.Info("Loading mod package: " + mod.Name + "...");
                    Directory.CreateDirectory(Game.GameFiles + "/Mods/" + Path.GetFileNameWithoutExtension(mod.Name));
                    try
                    {
                        // Load mod
                        FileStream strm = mod.OpenRead();
                        ModInfo info = manager.Load(strm, Path.GetFullPath(Game.GameFiles + "/Mods/" + Path.GetFileNameWithoutExtension(mod.Name)));

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
            }

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
                Thread th = new Thread(() => {
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
                        string[] parts = tkn.Split(".");
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
                                    impl.RefreshToken(res.Trim());
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
                                impl.RefreshFailure();
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
