using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Common;
using UnityEngine;

namespace Phoenix.Unity.PGL.Internal
{
    /// <summary>
    /// Game Information Implementation
    /// </summary>
    public class GameImpl : Game
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
            return Application.isEditor || Debug.isDebugBuild;
        }
    }
}