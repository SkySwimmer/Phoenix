using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Phoenix.Common.Logging;
using Phoenix.Unity.PGL.Internal.Packages;
using Phoenix.Common.IO;
using System.Linq;
using System.IO.Compression;
using Phoenix.Server;
using Application = UnityEngine.Application;
using AssetBundle = UnityEngine.AssetBundle;

namespace Phoenix.Unity.PGL.Mods
{
    /// <summary>
    /// Phoenix Mod Manager
    /// </summary>
    public class ModManager
    {
        private bool locked;
        private List<ModInfo> mods = new List<ModInfo>();
        private static Dictionary<string, Assembly> assemblyCache = new Dictionary<string, Assembly>();
        private Logger logger = Logger.GetLogger("Mod Manager");

        /// <summary>
        /// Unloads all mods
        /// </summary>
        internal void Unload()
        {
            mods.ForEach(t => t.Package.Close());
        }

        /// <summary>
        /// Locks the mod manager (internal)
        /// </summary>
        internal void Lock()
        {
            locked = true;
        }

        /// <summary>
        /// Locks the mod manager and finishes loading (internal)
        /// </summary>
        internal void LoadFinish()
        {
            locked = true;
            logger.Info("Post-initializing mods...");
            foreach (ModInfo mod in mods)
            {
                logger.Info("Post-initializing mod: " + mod.ID);
                mod.Instance.PostInit();
            }
            logger.Info("Mod loading finished.");
        }

        /// <summary>
        /// Loads a mod
        /// </summary>
        /// <param name="mod">Mod file stream</param>
        /// <param name="dataDir">Data directory</param>
        public ModInfo Load(FileStream mod, string dataDir)
        { 
            // TODO: client mod debugging
            // TODO: server mod loading
            
            if (locked)
                throw new ArgumentException("Locked");
            BinaryPackage package = new BinaryPackage(mod, Path.GetFileName(mod.Name), () => File.OpenRead(mod.Name));

            // Read mod information
            BinaryPackageEntry ent = package.GetEntry("clientmodmanifest.bin");
            if (ent == null)
                throw new ArgumentException("Invalid mod package: " + package.Name + ": missing mod manifest file! (entry clientmodmanifest.bin was not not found)");
            Stream binStrm = package.GetStream(ent);
            DataReader rd = new DataReader(binStrm);
            string id = rd.ReadString();
            string version = rd.ReadString();
            string type = rd.ReadString();
            binStrm.Close();
            logger.Info("Loading mod: " + id + ", version: " + version);
            ModInfo i = GetMod(id);
            if (i != null)
            {
                throw new ArgumentException("Duplicate mod found!\n" +
                    "Mod file: " + i.Package.Name + "\n" +
                    "Version: " + i.Version + "\n\n" +
                    "Mod file: " + mod.Name + "\n"
                    + "Version: " + version);
            }

            // Read mod assembly into memory
            logger.Debug("Loading mod assembly...");
            ent = package.GetEntry("clientassembly.bin");
            if (ent == null)
                throw new ArgumentException("Invalid mod package: " + package.Name + ": missing mod client assembly file!");
            binStrm = new GZipStream(package.GetStream(ent), CompressionMode.Decompress);
            MemoryStream data = new MemoryStream();
            binStrm.CopyTo(data);
            Assembly modAsm = Assembly.Load(data.ToArray());
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                AssemblyName nm = new AssemblyName(args.Name);
                if (nm.FullName == modAsm.GetName().FullName)
                    return modAsm;
                else
                {
                    // Check dependencies
                    lock (assemblyCache)
                    {
                        if (assemblyCache.ContainsKey(args.Name))
                            return assemblyCache[args.Name];
                    }

                    // Find in package
                    BinaryPackageEntry? ent = package.GetEntry("ClientDependencies/" + nm.Name + ".dll");
                    if (ent != null)
                    {
                        // Load assembly
                        GZipStream binStrm = new GZipStream(package.GetStream(ent), CompressionMode.Decompress);
                        MemoryStream data = new MemoryStream();
                        binStrm.CopyTo(data);
                        Assembly asm = Assembly.Load(data.ToArray());
                        lock (assemblyCache)
                            assemblyCache[args.Name] = asm;
                        binStrm.Close();
                        return asm;
                    }
                }
                return null;
            };
            binStrm.Close();
            try
            {
                logger.Debug("Loaded " + modAsm.GetTypes().Length + " types.");
            }
            catch
            {
                throw new ArgumentException("Invalid mod package: " + package.Name + ": assembly incompatible!");
            }

            // Load mod type
            PhoenixClientMod modInst;
            try
            {
                logger.Debug("Loading mod type...");
                logger.Debug("Instantiating type: " + type);
                modInst = (PhoenixClientMod)modAsm.GetType(type).GetConstructor(new Type[0]).Invoke(new object[0]);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Invalid mod package: " + package.Name + ": incompatible mod class!", e);
            }
            logger.Debug("Creating mod container...");
            ModInfo info = new ModInfo(id, version, package, modAsm, modInst);
            modInst.Setup(this, info, dataDir);
            mods.Add(info);

            // Add asset provider to phoenix
            AssetManager.AddProvider(new BinaryPackageAssetProvider(info.Package, "ServerAssets"));

            // Load asset bundles
            logger.Info("Loading asset bundles from " + info.ID + "...");
            string bundleSubDir = "";
            switch (Application.platform)
            {
                case UnityEngine.RuntimePlatform.OSXEditor:
                    bundleSubDir = "osx";
                    break;
                case UnityEngine.RuntimePlatform.OSXPlayer:
                    bundleSubDir = "osx";
                    break;
                case UnityEngine.RuntimePlatform.WindowsPlayer:
                    bundleSubDir = "win";
                    break;
                case UnityEngine.RuntimePlatform.WindowsEditor:
                    bundleSubDir = "win";
                    break;
                case UnityEngine.RuntimePlatform.Android:
                    bundleSubDir = "android";
                    break;
                case UnityEngine.RuntimePlatform.LinuxPlayer:
                    bundleSubDir = "linux";
                    break;
                case UnityEngine.RuntimePlatform.LinuxEditor:
                    bundleSubDir = "linux";
                    break;
                case UnityEngine.RuntimePlatform.LinuxServer:
                    bundleSubDir = "linux";
                    break;
                case UnityEngine.RuntimePlatform.WindowsServer:
                    bundleSubDir = "win";
                    break;
                case UnityEngine.RuntimePlatform.OSXServer:
                    bundleSubDir = "osx";
                    break;
            }
            foreach (BinaryPackageEntry entry in info.Package.GetEntriesIn("AssetBundles/" + bundleSubDir))
            {
                logger.Trace("Loading mod asset bundle: " + entry.Key + "...");
                AssetBundle bundle = AssetBundle.LoadFromStream(info.Package.GetStream(entry));
                modInst.AddBundle(bundle, entry.Key.Substring(("AssetBundles/" + bundleSubDir + "/").Length));
            }
            logger.Debug("Loading mod instance...");
            modInst.Load();
            return info;
        }

        /// <summary>
        /// Retrieves mods by ID
        /// </summary>
        /// <param name="id">Mod ID string</param>
        /// <returns>ModInfo instance or null</returns>
        public ModInfo GetMod(string id)
        {
            return mods.Where(t => t.ID == id).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves all mod packages
        /// </summary>
        /// <returns></returns>
        public ModInfo[] GetMods()
        {
            return mods.ToArray();
        }
    }

}
