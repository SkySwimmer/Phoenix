using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phoenix.Unity.PGL.Mods
{
    /// <summary>
    /// Phoenix Client Mod Abstract
    /// </summary>
    public abstract class PhoenixClientMod
    {
        private string modDir;
        private ModManager manager;
        private ModInfo modInfo;
        private Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>();

        internal void Setup(ModManager manager, ModInfo info, string modDir)
        {
            modInfo = info;
            this.modDir = modDir;
            this.manager = manager;
        }

        internal void AddBundle(AssetBundle bundle, string name)
        {
            bundles[name] = bundle;
        }

        /// <summary>
        /// Retrieves asset bundles
        /// </summary>
        /// <param name="name">Asset bundle file</param>
        /// <returns>AssetBundle instance</returns>
        public AssetBundle GetBundle(string name)
        {
            if (!bundles.ContainsKey(name))
                throw new ArgumentException("Bundle not found");
            return bundles[name];
        }

        /// <summary>
        /// Retrieves asset bundles
        /// </summary>
        public AssetBundle[] Bundles
        {
            get
            {
                return bundles.Values.ToArray();
            }
        }

        /// <summary>
        /// Retrieves asset bundle file names
        /// </summary>
        public string[] BundleNames
        {
            get
            {
                return bundles.Keys.ToArray();
            }
        }

        /// <summary>
        /// Retrieves the mod data directory (for eg. save data)
        /// </summary>
        public string DataDirectory
        {
            get
            {
                return modDir;
            }
        }

        /// <summary>
        /// Retrieves the mod package container
        /// </summary>
        public ModInfo ModInfo
        {
            get
            {
                return modInfo;
            }
        }

        /// <summary>
        /// Retrieves the mod manager
        /// </summary>
        protected ModManager ModManager
        {
            get
            {
                return manager;
            }
        }

        /// <summary>
        /// Called to load the mod
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// Called after all mods have been loaded
        /// </summary>
        public abstract void PostInit();

        /// <summary>
        /// Called on each frame update
        /// </summary>
        public virtual void Tick() {}
    }
}
