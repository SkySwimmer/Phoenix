using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Server.Components;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Phoenix.Debug.DebugServerRunner
{
    public class ServerBuilder
    {
        public static void Run(ProjectManifest project, Logger logger, DebugGameDefLib.DebugGameDef game)
        {
            // TODO: mass storage format for the assembly binary, individual encryption might be best
            // TODO: persistent key option
            // TODO: signatures or hash checks

            // Build
            logger.Level = LogLevel.INFO;

            // Get assets folder
            string assets = Path.GetFullPath(project.assetsFolder);

            // Log
            logger.Info("Preparing to build server...");

            // Create output
            logger.Debug("Creating output folder...");
            Directory.CreateDirectory("Build/Release");

            // Create assets output folder
            logger.Info("Compiling and encrypting assets...");
            if (Directory.Exists("Build/Release/Assets"))
                Directory.Delete("Build/Release/Assets", true);
            if (Directory.Exists("Build/Release/Components"))
                Directory.Delete("Build/Release/Components", true);

            // Pull all assets and build
            Dictionary<string, byte[]> keys = new Dictionary<string, byte[]>();
            Dictionary<string, byte[]> ivs = new Dictionary<string, byte[]>();
            Dictionary<string, string> assetFileNames = new Dictionary<string, string>();
            Dictionary<string, string> componentFileNames = new Dictionary<string, string>();
            logger.Debug("Finding assets...");
            AssetCompiler.LogLevel = LogLevel.INFO;
            BuildAssetsIn(assets, "", logger, keys, ivs, assetFileNames);

            // Build components
            logger.Info("Building server components...");
            logger.Debug("Creating component output directory...");
            Directory.CreateDirectory("Build/Release/Components");

            // Locate component assemblies
            logger.Debug("Locating component assemblies...");
            if (!File.Exists(project.assembliesDirectory + "/" + project.serverAssembly))
                throw new IOException("No compiled assemblies found!");
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                string file = new AssemblyName(args.Name).Name;
                if (File.Exists(project.assembliesDirectory + "/" + file + ".dll"))
                    return Assembly.LoadFile(Path.GetFullPath(project.assembliesDirectory + "/" + file + ".dll"));
                else if (File.Exists(project.assembliesDirectory + "/" + file + ".exe"))
                    return Assembly.LoadFile(Path.GetFullPath(project.assembliesDirectory + "/" + file + ".exe"));
                else if (File.Exists(project.assembliesDirectory + "/" + file))
                    return Assembly.LoadFile(Path.GetFullPath(project.assembliesDirectory + "/" + file));
                return null;
            };
            List<string> componentAssemblies = new List<string>();
            foreach (FileInfo file in new DirectoryInfo(project.assembliesDirectory).GetFiles("*.dll"))
            {
                // Load assembly
                logger.Debug("Searching assembly: " + file.FullName + " for components...");
                Assembly asm = Assembly.LoadFile(file.FullName);

                // Check types for components
                foreach (Type type in asm.GetTypes())
                {
                    if (typeof(Component).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                    {
                        componentAssemblies.Add(file.FullName);
                        logger.Info("Building component package for " + file.Name + "...");

                        // Compile assembly to a assembly package
                        // Find all component types
                        List<string> components = new List<string>();
                        foreach (Type t in asm.GetTypes())
                        {
                            if (typeof(Component).IsAssignableFrom(t))
                            {
                                components.Add(t.FullName);
                            }
                        }

                        // Create info binary
                        MemoryStream data = new MemoryStream();
                        DataWriter writer = new DataWriter(data);
                        writer.WriteString(file.Name);
                        writer.WriteString(asm.FullName);
                        writer.WriteInt(components.Count);
                        foreach (string component in components)
                            writer.WriteString(component);
                        
                        // Create component ID
                        string id = Guid.NewGuid().ToString().ToLower();
                        while (componentFileNames.ContainsKey(id))
                            id = Guid.NewGuid().ToString().ToLower();
                        componentFileNames[id] = file.Name;

                        // Setup encryption
                        logger.Debug("Creating AES encryption Key and IV...");
                        using (Aes aes = Aes.Create())
                        {
                            byte[] key = aes.Key;
                            byte[] iv = aes.IV;
                            keys[id] = key;
                            ivs[id] = (byte[])iv.Clone();
                            SHA256 hasher = SHA256.Create();
                            logger.Info("  Component ID: " + id);
                            logger.Info("  Component Key: " + string.Concat(key.Select(x => x.ToString("x2"))));
                            logger.Info("  Component IV: " + string.Concat(iv.Select(x => x.ToString("x2"))));
                            Stream s = File.OpenRead(file.FullName);
                            logger.Info("  Component Hash: " + string.Concat(hasher.ComputeHash(s).Select(x => x.ToString("x2"))));
                            s.Close();
                            hasher.Dispose();

                            // Write component
                            logger.Info("Encrypting and writing component...");
                            Stream outp = File.OpenWrite("Build/Release/Components/" + id + ".epcb");
                            ICryptoTransform tr = aes.CreateEncryptor(key, iv);
                            CryptoStream cryptStream = new CryptoStream(outp, tr, CryptoStreamMode.Write);
                            GZipStream gzip = new GZipStream(cryptStream, CompressionLevel.Fastest);
                            Stream strm = file.OpenRead();
                            strm.CopyTo(gzip);
                            strm.Close();

                            // Close stuff
                            gzip.Close();
                            cryptStream.Close();
                            outp.Close();
                            logger.Info("Written to Build/Release/Components/" + id + ".epcb");
                        }

                        break;
                    }
                }
            }

            // Compile other assemblies
            logger.Info("Compiling server assemblies...");
            logger.Debug("Finding assemblies...");
            BinaryPackageBuilder package = new BinaryPackageBuilder();
            List<string> assemblyNames = new List<string>();
            foreach (FileInfo file in new DirectoryInfo(project.assembliesDirectory).GetFiles("*.dll"))
            {
                if (!componentAssemblies.Contains(file.FullName))
                {
                    assemblyNames.Add(file.Name);

                    // Add entry
                    logger.Info("Adding " + file.Name + " to assemblies.epbp...");
                    package.AddEntry(file.Name, file.OpenRead());
                }
            }

            // Build package
            logger.Info("Compiling assemblies.epbp...");

            // Setup encryption
            logger.Debug("Creating AES encryption Key and IV...");
            using (Aes aes = Aes.Create())
            {
                byte[] key = aes.Key;
                byte[] iv = aes.IV;
                keys["@ROOT"] = key;
                ivs["@ROOT"] = (byte[])iv.Clone();
                logger.Info("  Binary Package Key: " + string.Concat(key.Select(x => x.ToString("x2"))));
                logger.Info("  Binary Package IV: " + string.Concat(iv.Select(x => x.ToString("x2"))));

                // Write component
                logger.Info("Encrypting and writing package...");
                Stream outp = File.OpenWrite("Build/Release/assemblies.epbp");
                ICryptoTransform tr = aes.CreateEncryptor(key, iv);
                CryptoStream cryptStream = new CryptoStream(outp, tr, CryptoStreamMode.Write);
                GZipStream gzip = new GZipStream(cryptStream, CompressionLevel.Fastest);
                package.Write(gzip);

                // Close stuff
                gzip.Close();
                cryptStream.Close();
                outp.Close();
                logger.Info("Written to Build/Release/assemblies.epbp");
            }

            // Build game asset
            logger.Info("Creating game info asset...");

            logger.Debug("Creating AES encryption Key and IV...");
            using (Aes aes = Aes.Create())
            {
                byte[] key = aes.Key;
                byte[] iv = aes.IV;
                keys["@GAMEMANIFEST"] = key;
                ivs["@GAMEMANIFEST"] = (byte[])iv.Clone();

                // Build manifest
                logger.Debug("Creating manifest entry...");
                MemoryStream man = new MemoryStream();
                DataWriter writer = new DataWriter(man);
                writer.WriteString(game.gameID);
                writer.WriteString(game.title);
                writer.WriteString(game.version);
                writer.WriteString(game.developmentStage);
                writer.WriteBoolean(game.hasOfflineSupport);
                writer.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); // Timestamp of build
                writer.WriteString(project.serverClass); // Server type
                writer.WriteString(project.serverAssembly.Replace(".dll", "").Replace(".exe", "")); // Server assembly
                byte[] data = man.ToArray();

                SHA256 hasher = SHA256.Create();
                logger.Info("  Asset Key: " + string.Concat(key.Select(x => x.ToString("x2"))));
                logger.Info("  Asset IV: " + string.Concat(iv.Select(x => x.ToString("x2"))));
                logger.Info("  Asset Hash: " + string.Concat(hasher.ComputeHash(data).Select(x => x.ToString("x2"))));
                hasher.Dispose();

                // Write asset
                logger.Info("Encrypting and writing asset...");
                Stream outp = File.OpenWrite("Build/Release/game.epaf");
                ICryptoTransform tr = aes.CreateEncryptor(key, iv);
                CryptoStream cryptStream = new CryptoStream(outp, tr, CryptoStreamMode.Write);
                GZipStream gzip = new GZipStream(cryptStream, CompressionLevel.Fastest);
                gzip.Write(data);

                // Close stuff
                gzip.Close();
                cryptStream.Close();
                outp.Close();
                logger.Info("Written to Build/Release/game.epaf");
            }

            // Create shuffled launch binary
            BinaryPackageBuilder launchInfoBin = new BinaryPackageBuilder();
            Dictionary<string, Stream> streams = new Dictionary<string, Stream>();

            // Add keys and ivs
            foreach ((string file, byte[] key) in keys)
                streams["Filekeys/" + file] = new MemoryStream(key);
            foreach ((string file, byte[] iv) in ivs)
                streams["Fileivs/" + file] = new MemoryStream(iv);
            
            // Add asset file ids
            foreach ((string id, string file) in assetFileNames)
                streams["Assets/" + file] = new MemoryStream(Encoding.UTF8.GetBytes(id));

            // Add component file ids
            foreach ((string id, string file) in componentFileNames)
                streams["Components/" + file] = new MemoryStream(Encoding.UTF8.GetBytes(id));

            // Add regular assemblies
            using (MemoryStream dInfo = new MemoryStream())
            {
                DataWriter writer = new DataWriter(dInfo);
                writer.WriteInt(assemblyNames.Count);
                foreach (string assembly in assemblyNames)
                    writer.WriteString(Path.GetFileName(assembly));
                streams["Assemblies"] = new MemoryStream(dInfo.ToArray());
            }

            // Add component assemblies
            using (MemoryStream dInfo = new MemoryStream())
            {
                DataWriter writer = new DataWriter(dInfo);
                writer.WriteInt(componentAssemblies.Count);
                foreach (string assembly in componentAssemblies)
                    writer.WriteString(Path.GetFileName(assembly));
                streams["ComponentAssemblies"] = new MemoryStream(dInfo.ToArray());
            }

            // Add streams
            Random rnd = new Random();
            while (streams.Count > 0)
            {
                KeyValuePair<string, Stream> strm = streams.ToArray()[rnd.Next(0, streams.Count)];
                launchInfoBin.AddEntry(strm.Key, strm.Value);
                streams.Remove(strm.Key);
            }

            // Build and encrypt
            logger.Info("Writing launch binary...");
            WriteEncryptedLaunchBinary(launchInfoBin);
            logger.Info("Written to Build/Release/launchinfo.bin");

            // Copy server runner
            logger.Info("Copying server runner...");
            if (File.Exists(AssemblyDirectory + "/Phoenix.Server.Bootstrapper.exe"))
                File.Copy(AssemblyDirectory + "/Phoenix.Server.Bootstrapper.exe", "Build/Release/server.exe", true);
            if (File.Exists(AssemblyDirectory + "/Phoenix.Server.Bootstrapper"))
                File.Copy(AssemblyDirectory + "/Phoenix.Server.Bootstrapper", "Build/Release/server", true);
            File.Copy(AssemblyDirectory + "/Phoenix.Server.Bootstrapper.dll", "Build/Release/Phoenix.Server.Bootstrapper.dll", true);
            File.Copy(AssemblyDirectory + "/Phoenix.Server.Bootstrapper.runtimeconfig.json", "Build/Release/Phoenix.Server.Bootstrapper.runtimeconfig.json", true);
            logger.Info("Done!");
        }

        // From stackoverflow: https://stackoverflow.com/a/283917
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private static void BuildAssetsIn(string folder, string pref, Logger logger, Dictionary<string, byte[]> keys, Dictionary<string, byte[]> ivs, Dictionary<string, string> assetFileNames)
        {
            // Find assets
            foreach (FileInfo file in new DirectoryInfo(folder).GetFiles())
            {
                if (!Directory.Exists("Build/Release/Assets"))
                {
                    logger.Debug("Creating asset output directory...");
                    Directory.CreateDirectory("Build/Release/Assets");
                }
                logger.Info("Building asset: " + pref + file.Name);

                // Create asset ID
                string id = Guid.NewGuid().ToString().ToLower();
                while (assetFileNames.ContainsKey(id))
                    id = Guid.NewGuid().ToString().ToLower();
                assetFileNames[id] = pref + file.Name;

                // Setup encryption
                logger.Debug("Creating AES encryption Key and IV...");
                using (Aes aes = Aes.Create())
                {
                    byte[] key = aes.Key;
                    byte[] iv = aes.IV;
                    keys[id] = key;
                    ivs[id] = (byte[])iv.Clone();
                    SHA256 hasher = SHA256.Create();
                    logger.Info("  Asset ID: " + id);
                    logger.Info("  Asset Key: " + string.Concat(key.Select(x => x.ToString("x2"))));
                    logger.Info("  Asset IV: " + string.Concat(iv.Select(x => x.ToString("x2"))));
                    Stream s = File.OpenRead(file.FullName);
                    logger.Info("  Asset Hash: " + string.Concat(hasher.ComputeHash(s).Select(x => x.ToString("x2"))));
                    s.Close();
                    hasher.Dispose();

                    // Compile asset
                    Stream strm = AssetCompiler.Compile(File.OpenRead(file.FullName), pref + file.Name);

                    // Write asset
                    logger.Info("Encrypting and writing asset...");
                    Stream outp = File.OpenWrite("Build/Release/Assets/" + id + ".epaf");
                    ICryptoTransform tr = aes.CreateEncryptor(key, iv);
                    CryptoStream cryptStream = new CryptoStream(outp, tr, CryptoStreamMode.Write);
                    GZipStream gzip = new GZipStream(cryptStream, CompressionLevel.Optimal);
                    strm.CopyTo(gzip);

                    // Close stuff
                    gzip.Close();
                    cryptStream.Close();
                    outp.Close();
                    strm.Close();
                    logger.Info("Written to Build/Release/Assets/" + id + ".epaf");
                }
            }
            foreach (DirectoryInfo dir in new DirectoryInfo(folder).GetDirectories())
            {
                // Recurse into subdirectories
                logger.Debug("Finding assets in " + pref + dir.Name + "...");
                BuildAssetsIn(dir.FullName, pref + dir.Name + "/", logger, keys, ivs, assetFileNames);
            }
        }

        private static Stream LaunchBinaryEncryptionStream(Stream target)
        {
            //
            // Implement this method for better security
            //

            //
            // WE HIGHLY RECOMMEND TO CREATE CUSTOM PROPRIETARY METHODS FOR BEST SECURITY
            //

            // The parameter 'target' contains the output file to which you want to write
            // You want to return some form of encryption stream that encrypts data as it writes

            // Please note that the format MUST be seekable as the server reads the file multiple times and seeks through it to find payload
            // Phoenix has hash checks for asset and component loading however you will need to do signature checks on the launch binary as else the hashses can be modified

            Console.Error.WriteLine();
            Console.Error.WriteLine();
            Console.Error.WriteLine();
            Console.Error.WriteLine("WARNING! Unencrypted launch binary file! Please fork the server builder (debug server runner) and implement this method for security, otherwise your assets will be easy to decrypt!");
            Console.Error.WriteLine("This code is kept in ServerBuilder.cs at the end of the file!");
            Console.Error.WriteLine();
            Console.Error.WriteLine();
            Console.Error.WriteLine();
            return target;
        }

        private static void WriteEncryptedLaunchBinary(BinaryPackageBuilder launchInfoBin)
        {
            //
            // Alternatively to the above, you can replace this writing code to use your own launch binary format with inner encryption
            // launchInfoBin.GetCurrentEntries() will return the collection of entries and their streams as well as if the streams should be closed on build
            //

            //
            // Ordinarily the format is as following:
            //
            // Base format:
            // 4 bytes - count of entries (little-endian signed integer)
            // headers - each entry header below
            // all entry data is added one-by-one after this, no length prefixes, no delimiters, nothing, the headers control how much should be read
            //
            // Entry header format:
            // 4 bytes - length prefix of key
            // string  - key string bytes (UTF-8)
            // 8 bytes - start of payload
            // 8 bytes - end of payload
            //

            //
            // Suggested replacement format:
            //
            // For security we recommend constructing some form of global key that is used to encrypt individual entries in
            // the launch binary, file-wide encryption can break as the file needs to be opened and seeked through multiple times to read keys and file ids
            //

            FileStream output = File.OpenWrite("Build/Release/launchinfo.bin");
            Stream dest = LaunchBinaryEncryptionStream(output);
            launchInfoBin.Write(dest);
            dest.Close();
            output.Close();
        }
    }
}