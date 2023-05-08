using Phoenix.Server.Bootstrapper.Packages;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Phoenix.Server.Bootstrapper
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Console.WriteLine("Preparing server... Please wait...");
            Environment.CurrentDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            if (!Directory.Exists("Components") || !File.Exists("assemblies.epbp") || !File.Exists("game.epaf") || !File.Exists("launchinfo.bin"))
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Critical Error!");
                Console.Error.WriteLine("Missing critical files required to start the server!");
                Console.Error.WriteLine("Please make sure the server can access all files in its directory.");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(1);
            }

            // Read launch info
            BinaryPackage launchPackage = LoadEncryptedLaunchBinary();

            // Read game info
            DataReader reader = new DataReader(Decrypt(File.OpenRead("game.epaf"), "@GAMEMANIFEST", launchPackage));
            string gameID = reader.ReadString();
            string title = reader.ReadString();
            string version = reader.ReadString();
            string stage = reader.ReadString();
            bool offlineSupport = reader.ReadBoolean();
            long buildtime = reader.ReadLong();
            string serverClass = reader.ReadString();
            string serverAssembly = reader.ReadString();
            reader.GetStream().Close();

            // Load assemblies
            Dictionary<string, Assembly> knownAssemblies = new Dictionary<string, Assembly>();
            BinaryPackage assemblies = LoadTypicalPackage(File.OpenRead("assemblies.epbp"), "@ROOT", launchPackage);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                string file = new AssemblyName(args.Name).Name;
                if (knownAssemblies.ContainsKey(file))
                    return knownAssemblies[file];
                if (assemblies.GetEntry(file + ".dll") != null)
                {
                    Assembly res= Assembly.Load(ReadBytesFromEntry(assemblies.GetEntry(file + ".dll"), assemblies));
                    knownAssemblies[file] = res;
                    return res;
                }
                else if (assemblies.GetEntry(file + ".exe") != null)
                {
                    Assembly res = Assembly.Load(ReadBytesFromEntry(assemblies.GetEntry(file + ".exe"), assemblies));
                    knownAssemblies[file] = res;
                    return res;
                }
                else if (assemblies.GetEntry(file) != null)
                {
                    Assembly res = Assembly.Load(ReadBytesFromEntry(assemblies.GetEntry(file), assemblies));
                    knownAssemblies[file] = res;
                    return res;
                }
                return null;
            };

            // Load components
            DataReader componentInfoReader = new DataReader(new MemoryStream(ReadBytesFromEntry(launchPackage.GetEntry("ComponentAssemblies"), launchPackage)));
            int l = componentInfoReader.ReadInt();
            for (int i = 0; i < l; i++)
            {
                string fileName = componentInfoReader.ReadString();
                string componentId = Encoding.UTF8.GetString(ReadBytesFromEntry(launchPackage.GetEntry("Components/" + fileName), launchPackage));
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    string file = new AssemblyName(args.Name).Name;
                    if (knownAssemblies.ContainsKey(file))
                        return knownAssemblies[file];
                    if (fileName == file + ".dll")
                    {
                        Stream data = File.OpenRead("Components/" + componentId + ".epcb");
                        Stream dec = Decrypt(data, componentId, launchPackage);
                        DataReader rd = new DataReader(dec);
                        Assembly asm = Assembly.Load(rd.ReadAllBytes());
                        data.Close();
                        knownAssemblies[file] = asm;
                        return asm;
                    }
                    else if (fileName == file + ".exe")
                    {
                        Stream data = File.OpenRead("Components/" + componentId + ".epcb");
                        Stream dec = Decrypt(data, componentId, launchPackage);
                        DataReader rd = new DataReader(dec);
                        Assembly asm = Assembly.Load(rd.ReadAllBytes());
                        data.Close();
                        knownAssemblies[file] = asm;
                        return asm;
                    }
                    return null;
                };
            }

            // Start
            ServerRunner.StartServer(gameID, title, version, stage, offlineSupport, serverClass, serverAssembly, launchPackage, args);
        }

        public static BinaryPackage LoadTypicalPackage(Stream strm, string name, BinaryPackage launchPackage)
        {
            return LoadPackage(Decrypt(strm, name, launchPackage), name);
        }

        public static BinaryPackage LoadPackage(Stream strm, string name)
        {
            DataReader rd = new DataReader(strm);
            byte[] data = rd.ReadAllBytes();
            strm.Close();
            BinaryPackage package = new BinaryPackage(new MemoryStream(data), name, () =>
            {
                return new MemoryStream(data);
            });
            return package;
        }

        public static Stream Decrypt(Stream data, string name, BinaryPackage launchPackage)
        {
            Aes aes = Aes.Create();
            aes.Key = ReadBytesFromEntry(launchPackage.GetEntry("Filekeys/" + name), launchPackage);
            aes.IV = ReadBytesFromEntry(launchPackage.GetEntry("Fileivs/" + name), launchPackage);

            // Create decryptor
            return new GZipStream(new EncryptedStream(data, aes, aes.CreateDecryptor(), CryptoStreamMode.Read), CompressionMode.Decompress);
        }

        public static byte[] ReadBytesFromEntry(BinaryPackageEntry entry, BinaryPackage package)
        {
            Stream strm = package.GetStream(entry);
            DataReader rd = new DataReader(strm);
            byte[] data = rd.ReadAllBytes();
            strm.Close();
            return data;
        }

        private static Stream LaunchBinaryDecryptionStream(Stream source)
        {
            //
            // Implement this method for better security
            //

            //
            // WE HIGHLY RECOMMEND TO CREATE CUSTOM PROPRIETARY METHODS FOR BEST SECURITY
            //

            // The parameter 'source' contains the launch binary raw file stream from which you can read
            // You want to return some form of decryption stream that decrypts data as it reads

            // NOTE THAT IT IS REQUIRED FOR IT TO BE ABLE TO BE SEEKED THROUGH
            // Secondly, please make sure that the source stream is closed when the stream you return is closed else a memory leak will arise

            // Phoenix has hash checks for asset and component loading however you will need to do signature checks on the launch binary as else the hashses can be modified

            Console.Error.WriteLine("WARNING! Unencrypted launch binary file! Please fork the bootstrapper and implement this method for security, otherwise your assets will be easy to decrypt!");
            Console.Error.WriteLine("This code is kept in Program.cs at the end of the file!");
            return source;
        }

        private static BinaryPackage LoadEncryptedLaunchBinary()
        {
            //
            // Alternatively to the above, you can replace this loading code to use your own launch binary format with inner encryption
            //

            Stream launchSource = File.OpenRead("launchinfo.bin");
            Stream launchData = LaunchBinaryDecryptionStream(launchSource);
            return new BinaryPackage(launchData, "launchinfo.bin", () =>
            {  
                Stream launchSource = File.OpenRead("launchinfo.bin");
                Stream launchData = LaunchBinaryDecryptionStream(launchSource);
                return launchData;
            });
        }

    }
}