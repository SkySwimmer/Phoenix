using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Phoenix.Client.IntegratedServerBootstrapper;
using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Server;
using Phoenix.Server.Components;
using Phoenix.Unity.PGL.Internal.Packages;

namespace Phoenix.Unity.PGL.Mods.IntegratedServerSupport
{
    /// <summary>
    /// Support wrapper for integrated servers that will load server mods next to client mods if integrated servers are supported. 
    /// You may want to remove this class if you do not have servers built into your client, it will not affect the modloader.
    /// </summary>
    public static class ModIntegratedServerSupportBindings
    {
        private static List<ServerModInfo> mods = new List<ServerModInfo>();
        private static Dictionary<string, Assembly> assemblyCache = new Dictionary<string, Assembly>();
        
        private class ServerModInfo
        {
            private string id;
            private string version;
            private BinaryPackage package;
            private Assembly assembly;

            public ServerModInfo(string id, string version, BinaryPackage package, Assembly assembly)
            {
                this.id = id;
                this.version = version;
                this.package = package;
                this.assembly = assembly;
            }

            /// <summary>
            /// Retrieves the mod ID
            /// </summary>
            public string ID { 
                get 
                {
                    return id;
                }
            }

            /// <summary>
            /// Retrieves the mod version string
            /// </summary>
            public string Version
            {
                get
                {
                    return version;
                }
            }
            
            /// <summary>
            /// Retrieves the mod binary package
            /// </summary>
            public BinaryPackage Package
            {
                get
                {
                    return package;
                }
            }

            /// <summary>
            /// Retrieves the mod assembly
            /// </summary>
            public Assembly Assembly
            {
                get
                {
                    return assembly;
                }
            }
        }

        /// <summary>
        /// Retrieves mods by ID
        /// </summary>
        /// <param name="id">Mod ID string</param>
        /// <returns>ModInfo instance or null</returns>
        private static ServerModInfo GetMod(string id)
        {
            return mods.Where(t => t.ID == id).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves all mod packages
        /// </summary>
        /// <returns></returns>
        private static ServerModInfo[] GetMods()
        {
            return mods.ToArray();
        }

        /// <summary>
        /// Binds support hooks
        /// </summary>
        public static void Bind(ModManager manager)
        {
            Logger _logger = Logger.GetLogger("Mod Manager");

            // Bind mod loading
            manager.OnLoadModInternal += (id, version, type, package, mod) =>
            {
                // Read mod information
                BinaryPackageEntry ent = package.GetEntry("clientmodmanifest.bin");
                Stream binStrm = package.GetStream(ent);
                DataReader rd = new DataReader(binStrm);
                rd.ReadString();
                rd.ReadString();
                rd.ReadString();

                // If there is another boolean after that, it means the mod has configured if the bundled server mod is compatible with the integrated mod
                bool bundledIntegratedServerMod = false;
                try
                {
                    bundledIntegratedServerMod = rd.ReadBoolean();
                }
                catch { }
                binStrm.Close();

                // Find the entry for the integrated server
                if (bundledIntegratedServerMod)
                    ent = package.GetEntry("manifest.bin");
                else
                    ent = package.GetEntry("integratedservermodmanifest.bin");
                
                // Load integrated server mod
                if (ent != null)
                {
                    // Load information
                    binStrm = package.GetStream(ent);
                    rd = new DataReader(binStrm);
                    id = rd.ReadString();
                    version = rd.ReadString();
                    binStrm.Close();
                    _logger.Info("Loading bundled integrated server mod: " + id + ", version: " + version + ", parent client mod: " + mod.ID);

                    // Check if loaded
                    ServerModInfo i = GetMod(id);
                    if (i != null)
                    {
                        throw new ArgumentException("Duplicate server mod found!\n" +
                            "Mod id: " + id + "\n\n" +
                            "Mod file: " + i.Package.Name + "\n" +
                            "Version: " + i.Version + "\n\n" +
                            "Mod file: " + package.Name + "\n"
                            + "Version: " + version);
                    }
                                        
                    // Read mod assembly into memory
                    _logger.Debug("Loading mod assembly...");
                    ent = bundledIntegratedServerMod ? package.GetEntry("assembly.bin") : package.GetEntry("integratedserverassembly.bin");
                    if (ent == null)
                        throw new ArgumentException("Invalid mod package: " + package.Name + ": missing server mod assembly file!");
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
                            BinaryPackageEntry ent = package.GetEntry((bundledIntegratedServerMod ? "Dependencies" : "IntegratedServerDependencies") + "/" + nm.Name + ".dll");
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
                        _logger.Debug("Loaded " + modAsm.GetTypes().Length + " types.");
                    }
                    catch
                    {
                        throw new ArgumentException("Invalid mod package: " + package.Name + ": server mod assembly incompatible!");
                    }

                    ServerModInfo info = new ServerModInfo(id, version, package, modAsm);
                    mods.Add(info);
                }
            };

            // Bind server loading
            PhoenixIntegratedServer.OnCreateServer += (server) =>
            {
                // Load components
                _logger.Debug("Adding mod components to server...");
                foreach (ServerModInfo mod in GetMods())
                {
                    _logger.Info("Loading mod components from " + mod.ID + "...");
                    Type[] types = mod.Assembly.GetTypes();
                    foreach (Type t in types)
                    {
                        if (t.GetCustomAttribute<ModComponent>() != null)
                        {
                            _logger.Debug("Loading component type: " + t.Name + "...");
                            if (!typeof(Component).IsAssignableFrom(t) && !typeof(IComponentPackage).IsAssignableFrom(t))
                            {
                                _logger.Error("Could not load mod component: " + t.FullName + ", mod: " + mod.Package.Name + ": not a server component or package!");
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

                                // Add to server
                                server.AddComponent(comp);
                            }
                            else
                            {
                                IComponentPackage comp = (IComponentPackage)constr.Invoke(new object[0]);

                                // Add to server
                                server.AddComponentPackage(comp);
                            }
                        }
                    }
                }
            };
        }
    }
}