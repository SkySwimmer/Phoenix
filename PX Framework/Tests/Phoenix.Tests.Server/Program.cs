using System;
using System.Net;
using System.Net.Sockets;
using Phoenix.Common.Certificates;
using Phoenix.Common.Events;
using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Impl;
using Phoenix.Common.Networking.Registry;
using Phoenix.Server;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Phoenix.Server.Packages;
using Phoenix.Common;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Phoenix.Tests.Server
{
    public class Program : IEventListenerContainer
    {

        private static byte[] pglSecKey;
        private static byte[] pglSecIV;

        private class GameImplCl : Game
        {
            private static string _title;
            private static string _gameId;
            private static string _gameVersion;
            private static string _gameStage;
            private static string _session;
            private static bool _offlineSupport;
            private static string _playerData;
            private static string _saveData;
            private static string _assetsFolder;
            private static string _gameFolder;

            public void Register(Dictionary<string, string> data)
            {
                Implementation = this;
                if (_title != null)
                    return;
                _title = data["Game-Title"];
                _gameId = data["Game-ID"];
                _gameVersion = data["Game-Version"];
                _gameStage = data["Game-Channel"];
                _session = data["Session"];
                if (_session == "OFFLINE")
                    _session = null;
                _offlineSupport = data["Offline-Support"] == "True";
                _assetsFolder = Path.GetFullPath(data["Assets-Path"]);
                _gameFolder = Path.GetFullPath(data["Game-Storage-Path"]);
                _playerData = Path.GetFullPath(data["Player-Data-Path"]);
                _saveData = Path.GetFullPath(data["Save-Data-Path"]);
            }

            public void RefreshToken(string token)
            {
                _session = token;
            }

            public void RefreshFailure()
            {
                _session = null;
            }

            public override string GetAssetsFolder()
            {
                return _assetsFolder;
            }

            public override string GetDevelopmentStage()
            {
                return _gameStage;
            }

            public override string GetGameFiles()
            {
                return _gameFolder;
            }

            public override string GetGameID()
            {
                return _gameId;
            }

            public override string GetPlayerData()
            {
                return _playerData;
            }

            public override string GetSaveData()
            {
                return _saveData;
            }

            public override string GetSessionToken()
            {
                return _session;
            }

            public override string GetTitle()
            {
                return _title;
            }

            public override string GetVersion()
            {
                return _gameVersion;
            }

            public override bool HasOfflineSupport()
            {
                return _offlineSupport;
            }

            public override bool IsCurrentlyOffline()
            {
                // Check token
                if (_session == null)
                    return true;

                // Check interface
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    _session = null;
                    return true;
                }

                // Connected (at least, for now, checks are done by the token refresh in PhoenixPGL)
                return false;
            }

            public override bool IsDebugMode()
            {
                return true;
            }
        }

        private class FileAssetProvider : IAssetProvider
        {
            public string[] GetAssetsIn(string folder)
            {
                if (Directory.Exists("../../../Assets/" + folder))
                {
                    List<string> assets = new List<string>();
                    foreach (FileInfo file in new DirectoryInfo("../../../Assets/" + folder).GetFiles())
                    {
                        assets.Add(file.Name);
                    }
                    return assets.ToArray();
                }
                return new string[0];
            }

            public Stream? GetAssetStream(string asset)
            {
                if (File.Exists("../../../Assets/" + asset))
                    return File.OpenRead("../../../Assets/" + asset);
                else
                    return null;
            }
        }

        private static class PlayerPrefs
        {

            public static bool HasKey(string key)
            {
                return File.Exists("testdata-" + key);
            }

            public static string GetString(string key)
            {
                if (!File.Exists("testdata-" + key))
                    return null;
                return File.ReadAllText("testdata-" + key);
            }

            public static void SetString(string key, string value)
            {
                File.WriteAllText("testdata-" + key, value);
            }

        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Phoenix Library Tests: Generic Server Test is starting...");
            Logger.GlobalLogLevel = LogLevel.DEBUG;

            // test
            Logger _logger = Logger.GetLogger("test");

            // Parse arguments
            _logger.Info("Parsing arguments...");
            Dictionary<string, string> arguments = new Dictionary<string, string>();
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
                            return;
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
                        Logger.GlobalLogLevel = LogLevel.TRACE;
                        break;
                    case "debug":
                        Logger.GlobalLogLevel = LogLevel.DEBUG;
                        break;
                    default:
                        _logger.Fatal("Invalid argument: --loglevel: invalid value.");
                        return;
                }
            }

            // Init assets
            AssetManager.AddProvider(new FileAssetProvider());

            // Test
            _logger.Info("Loading game descriptor...");
            string descriptor;
            try
            {
                descriptor = AssetManager.GetAssetString("game.info").Replace("\r", "");
            }
            catch
            {
                _logger.Fatal("Missing game descriptor! Please add an asset named game.info in Assets/Resources/PhoenixAssets!");
                _logger.Fatal("Unable to continue, exiting game...");
                return;
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
                    _logger.Fatal("Unable to continue, exiting game...");
                    return;
                }
            }
            _logger.Trace("Verifying game descriptor...");
            if (!game.ContainsKey("Game-ID") || !game.ContainsKey("Game-Title"))
            {
                _logger.Fatal("Invalid game descriptor! Please modify game.info and include at least the following fields: Game-ID, Game-Title.");
                _logger.Fatal("Unable to continue, exiting game...");
                return;
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

            // Check support for configurations without a functional API server
            bool canRunWithoutAPI = (game.ContainsKey("Fallback-To-Embedded-Descriptor") && game["Fallback-To-Embedded-Descriptor"].ToLower() == "true");

            // Production client, we need to load the game document

            // Check arguments
            _logger.Trace("Verifying arguments...");
            if (arguments.ContainsKey("activate") && !arguments.ContainsKey("productkey"))
            {
                _logger.Fatal("Missing 'productkey' argument, unable to activate product.");
                return;
            }

            // Prepare folders
            _logger.Trace("Preparing folders...");
            string phoenixRoot = "phoenixroottest";
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
                    string url = PhoenixEnvironment.DefaultAPIServer;
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
                            _logger.Fatal("Failed to activate product! Product key was invalid!");
                            return;
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
                    _logger.Fatal("Failed to activate product! Please verify the connection with the server and the product key!");
                    return;
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
                string url = PhoenixEnvironment.DefaultAPIServer;
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
                        return;
                    }
                    else if (r.ReasonPhrase == "Missing DRM")
                    {
                        // Invalid DRM
                        _logger.Fatal("Failed to authenticate the game! Missing DRM!");
                        return;
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
                    if (!canRunWithoutAPI)
                    {
                        // Error
                        _logger.Fatal("Failed to authenticate the game! Please verify the connection with the server!");
                        return;
                    }
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
                        if (!canRunWithoutAPI)
                        {
                            // Error
                            _logger.Fatal("Failed to authenticate the game and local data was not available! Please verify the connection with the server!");
                            return;
                        }
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
            string gameRoot = ".";

            // Set up environment
            _logger.Info("Setting up Phoenix environment...");
            game["Assets-Path"] = phoenixRoot + "/assets/" + game["Game-Channel"] + "/" + game["Game-Version"] + "/" + game["Asset-Identifier"];
            game["Game-Storage-Path"] = gameRoot;
            game["Player-Data-Path"] = phoenixRoot + "/playerdata/" + game["Game-ID"];
            game["Save-Data-Path"] = phoenixRoot + "/savedata/" + game["Game-ID"];
            if (!game.ContainsKey("Refresh-Endpoint"))
            {
                string urlA = PhoenixEnvironment.DefaultAPIServer;
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
            GameImplCl impl = new GameImplCl();
            impl.Register(game);

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
                    return;
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

                                string url = PhoenixEnvironment.DefaultAPIServer;
                                if (!url.EndsWith("/"))
                                    url += "/";
                                if (!cl.GetAsync(url + "servers").GetAwaiter().GetResult().IsSuccessStatusCode)
                                    throw new Exception();
                            }
                            catch
                            {
                                _logger.Warn("Lost internet connection");
                                impl.RefreshFailure();
                                break;
                            }
                        }

                        string tkn = Game.SessionToken;
                        if (!NetworkInterface.GetIsNetworkAvailable() || tkn == null)
                        {
                            _logger.Warn("Lost internet connection");
                            impl.RefreshFailure();
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


            // Create channel registry
            ChannelRegistry registry = new ChannelRegistry();
            registry.Register(new TestChannel());

            // Create connections
            ConnectionBundle bundle = Connections.CreateIntegratedConnections(registry);
            Connection client = bundle.Client;
            ServerConnection server = bundle.Server;
            server.Open();

            // Attach events
            client.Connected += (t, a) =>
            {
                Logger.GetLogger("TEST").Info("Server connection established");
            };
            server.Connected += (t, a) =>
            {
                Logger.GetLogger("TEST").Info("Client connected");
            };
            client.Disconnected += (t, r, a) =>
            {
                Logger.GetLogger("TEST").Info("Server connection closed");
            };
            server.Disconnected += (t, r, a) =>
            {
                Logger.GetLogger("TEST").Info("Client disconnected");
            };

            // Connect
            client.Open();

            // Send test packet
            TestChannel ch = client.GetChannel<TestChannel>();
            ch.SendPacket(new TestPacket()
            {
                Sender = "Phoenix Test Server",
                Message = "Hello World"
            });

            // Disconnect
            server.Close();

            // Test networked
            Connection serverConn = Connections.CreateNetworkServer(12345, registry, null);
            Connection connClient = Connections.CreateNetworkClient("127.0.0.1", 12345, registry, null);

            // Start
            serverConn.Open();
            connClient.Open();

            // Attach events
            connClient.Connected += (t, a) =>
            {
                Logger.GetLogger("TEST").Info("Server connection established");
            };
            serverConn.Connected += (t, a) =>
            {
                Logger.GetLogger("TEST").Info("Client connected");
            };
            connClient.Disconnected += (t, r, a) =>
            {
                Logger.GetLogger("TEST").Info("Server connection closed");
            };
            serverConn.Disconnected += (t, r, a) =>
            {
                Logger.GetLogger("TEST").Info("Client disconnected");
            };

            // Send test packet
            ch = connClient.GetChannel<TestChannel>();
            ch.SendPacket(new TestPacket()
            {
                Sender = "Phoenix Test Server",
                Message = "Hello World"
            });

            // Ping tests
            Logger log = Logger.GetLogger("TEST");
            for (int i = 0; i < 10; i++)
            {
                PingPacket ping = new PingPacket();
                long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                PongPacket? pong = ch.SendPacketAndWaitForResponse<PongPacket>(ping);
                log.Info("Time: " + (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start));
            }

            // Disconnect
            connClient.Close();
            serverConn.Close();
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

    class GameImpl : Game
    {
        public void Register()
        {
            Implementation = this;
        }

        public override string GetAssetsFolder()
        {
            return "Assets";
        }

        public override string GetDevelopmentStage()
        {
            return "Alpha";
        }

        public override string GetGameFiles()
        {
            return "Assets";
        }

        public override string GetGameID()
        {
            return "test";
        }

        public override string GetPlayerData()
        {
            return "Playerdata";
        }

        public override string GetSessionToken()
        {
            return null;
        }

        public override string GetTitle()
        {
            return "Test Project";
        }

        public override string GetVersion()
        {
            return "1.0.0";
        }

        public override bool HasOfflineSupport()
        {
            return false;
        }

        public override bool IsCurrentlyOffline()
        {
            return false;
        }

        public override bool IsDebugMode()
        {
            return true;
        }

        public override string GetSaveData()
        {
            return "SaveData";
        }
    }
}