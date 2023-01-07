using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phoenix.Common
{
    public static class Game
    {
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

        private class PXToken
        {
            public long exp;
        }

        private static string _title;
        private static string _gameId;
        private static string _gameVersion;
        private static string _gameStage;
        private static string _productKey;
        private static string _digitalSeal;
        private static string _session;
        private static bool _offlineSupport;
        private static string _playerData;
        private static string _assetsFolder;
        private static string _gameFolder;

        /// <summary>
        /// Game title
        /// </summary>
        public static string Title => _title;

        /// <summary>
        /// Game ID
        /// </summary>
        public static string GameID => _gameId;

        /// <summary>
        /// Current game version
        /// </summary>
        public static string Version => _gameVersion;

        /// <summary>
        /// Current game development stage
        /// </summary>
        public static string DevelopmentStage => _gameStage;

        /// <summary>
        /// Game session token (for contacting the login servers)
        /// </summary>
        public static string SessionToken => _session;

        /// <summary>
        /// Offline support, true if enabled, false if unsupported
        /// </summary>
        public static bool OfflineSupport => _offlineSupport;

        /// <summary>
        /// Shows if the device is currently offline
        /// </summary>
        public static bool IsOffline => _session == null;

        /// <summary>
        /// Player data folder
        /// </summary>
        public static string PlayerData => _playerData;

        /// <summary>
        /// Assets folder
        /// </summary>
        public static string AssetsFolder => _assetsFolder;

        /// <summary>
        /// Game file folder
        /// </summary>
        public static string GameFiles => _gameFolder;

        /// <summary>
        /// Product key
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string ProductKey => _productKey;

        /// <summary>
        /// Digital seal
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string DigitalSeal => _digitalSeal;

        /// <summary>
        /// INTERNAL USE ONLY
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitTokenRefresh(string session, string refreshEndpoint)
        {
            if (_title != null && _productKey == null)
                return;

            if (session.Split(".").Length == 3)
            {
                Thread th = new Thread(() => { 
                    while (true)
                    {
                        // Check generic connection
                        if (!NetworkInterface.GetIsNetworkAvailable())
                        {
                            // Offline
                            _session = null;
                        }

                        // Check expiry
                        string payload = Encoding.UTF8.GetString(Base64Url.Decode(session.Split(".")[1]));
                        PXToken token = JsonConvert.DeserializeObject<PXToken>(payload);
                        if (token.exp - (5 * 60) <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        {
                            // Contact refresh endpoint
                            string newToken = Utils.DownloadString(refreshEndpoint, "GET", null, new Dictionary<string, string>()
                            {
                                ["Authorization"] = "Bearer " + session
                            });

                            // Check result
                            if (newToken != null && session != "")
                            {
                                // Save new token
                                _session = newToken;
                                session = newToken;
                            } 
                            else
                            {
                                // Handle failure
                                _session = null;

                                // Check offline support
                                if (!OfflineSupport)
                                {
                                    Debug.LogError("Lost connection to the Phoenix servers! Unable to refresh authorization token!");
                                    Debug.LogError("Game does not support offline play, terminated refresher.");
                                    return;                                   
                                }

                                // Wait 30 seconds
                                Thread.Sleep(30000);
                                continue;
                            }
                        }
                        Thread.Sleep(1000);
                    }
                });
                th.IsBackground = true;
                th.Name = "Phoenix Refresher";
                th.Start();
            }
        }

        /// <summary>
        /// INTERNAL USE ONLY
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(Dictionary<string, string> data)
        {
            if (_title != null && _productKey == null)
                return;
            _title = data["Game-Title"];
            _gameId = data["Game-ID"];
            _gameVersion = data["Game-Version"];
            _gameStage = data["Game-Channel"];
            _session = data["Session"];
            if (_session == "OFFLINE")
                _session = null;
            _offlineSupport = data["Offline-Support"] == "True";
            _productKey = data["Product-Key"];
            _digitalSeal = data["Digital-Seal"];
            _assetsFolder = data["Assets-Path"];
            _gameFolder = data["Game-Storage-Path"];
            _playerData = data["Player-Data-Path"];
        }

        /// <summary>
        /// INTERNAL USE ONLY
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Clean()
        {
            _productKey = null;
            _digitalSeal = null;
        }
    }
}
