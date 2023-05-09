using System.IO;
using System.IO.Compression;
using Phoenix.Server.Bootstrapper.Packages;

namespace Phoenix.Debug.BinPackageTool
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 3 || (args[0].ToLower() != "unpack" && args[0].ToLower() != "pack"))
            {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("pbputil pack \"<source folder>\" \"<destination file>\"");
                Console.Error.WriteLine("pbputil unpack \"<source file>\" \"<destination folder>\"");
                Environment.Exit(1);
            }
            switch (args[0].ToLower())
            {
                case "pack":
                    {
                        if (!Directory.Exists(args[1]))
                        {
                            Console.Error.WriteLine("Error: source folder does not exist");
                            Environment.Exit(1);
                        }
                        if (File.Exists(args[2]))
                        {
                            Console.Error.WriteLine("Warning: destination file already exists! It will be overwritten!");
                            if (args.Length < 4 || args[3] != "confirm")
                            {
                                Console.Error.WriteLine("To continue anyways, add 'confirm' to the command.");
                                Environment.Exit(1);
                            }
                        }
                        BinaryPackageBuilder builder = new BinaryPackageBuilder();
                        Console.WriteLine("Packing " + Path.GetFullPath(args[1]) + "...");
                        List<string> compressExclude = new List<string>();
                        List<string> compressInclude = new List<string>();
                        List<string> exclude = new List<string>();
                        List<string> include = new List<string>();
                        bool compressAll = false;
                        if (File.Exists(args[1] + "/pbputil.package.conf"))
                        {
                            foreach (string confLine in File.ReadAllLines(args[1] + "/pbputil.package.conf"))
                            {
                                string command = confLine;
                                string argsStr = "";
                                if (command.Contains(" "))
                                {
                                    argsStr = command.Substring(command.IndexOf(" ") + 1);
                                    command = command.Remove(command.IndexOf(" "));
                                }

                                if (command == "compress")
                                {
                                    if (argsStr != "all" && argsStr != "selective")
                                        Console.Error.WriteLine("Warning: invalid compression directive: " + command + " " + argsStr + ": expected either 'compress all' or 'compress selective'");
                                    else
                                        compressAll = argsStr == "all";
                                }
                                else if (command == "exclude")
                                    exclude.Add(argsStr);
                                else if (command == "include")
                                    include.Add(argsStr);
                                else if (command == "compress-exclude")
                                    compressExclude.Add(argsStr);
                                else if (command == "compress-include")
                                    compressInclude.Add(argsStr);
                            }
                        }
                        void Scan(DirectoryInfo info, string pref)
                        {
                            foreach (DirectoryInfo dir in info.GetDirectories())
                                Scan(dir, pref + dir.Name + "/");
                            foreach (FileInfo file in info.GetFiles())
                            {
                                if (pref == "" && file.Name == "pbputil.package.conf")
                                    continue;
                                if ((!exclude.Any(t => t.ToLower() == (pref + file.Name).ToLower()) && !exclude.Any(t => t.EndsWith("/") && (pref.ToLower() == t.ToLower() || pref.ToLower().StartsWith(t.ToLower()))) && !exclude.Any(t => t.EndsWith("/*") && (pref.ToLower() == t.ToLower().Remove(t.Length - 1) || pref.ToLower().StartsWith(t.ToLower().Remove(t.Length - 1))))) || (include.Any(t => t.ToLower() == (pref + file.Name).ToLower()) || include.Any(t => t.EndsWith("/") && (pref.ToLower() == t.ToLower() || pref.ToLower().StartsWith(t.ToLower()))) || include.Any(t => t.EndsWith("/*") && (pref.ToLower() == t.ToLower().Remove(t.Length - 1) || pref.ToLower().StartsWith(t.ToLower().Remove(t.Length - 1))))))
                                {
                                    Console.WriteLine("Adding: " + pref + file.Name);
                                    Stream strm = File.OpenRead(file.FullName);
                                    if (compressAll || compressInclude.Any(t => t.ToLower() == (pref + file.Name).ToLower()) || compressInclude.Any(t => t.EndsWith("/") && (pref.ToLower() == t.ToLower() || pref.ToLower().StartsWith(t.ToLower()))) || compressInclude.Any(t => t.EndsWith("/*") && (pref.ToLower() == t.ToLower().Remove(t.Length - 1) || pref.ToLower().StartsWith(t.ToLower().Remove(t.Length - 1)))))
                                    {
                                        // Compression enabled, check exclusions
                                        if (!compressExclude.Any(t => t.ToLower() == (pref + file.Name).ToLower()) && !compressExclude.Any(t => t.EndsWith("/") && (pref.ToLower() == t.ToLower() || pref.ToLower().StartsWith(t.ToLower()))) && !compressExclude.Any(t => t.EndsWith("/*") && (pref.ToLower() == t.ToLower().Remove(t.Length - 1) || pref.ToLower().StartsWith(t.ToLower().Remove(t.Length - 1)))))
                                        {
                                            // Copy to memory
                                            MemoryStream mem = new MemoryStream();
                                            GZipStream gzip = new GZipStream(mem, CompressionLevel.Fastest);
                                            strm.CopyTo(gzip);
                                            gzip.Close();
                                            strm.Close();

                                            // Set from memory
                                            strm = new MemoryStream(mem.ToArray());
                                        }
                                    }
                                    builder.AddEntry(pref + file.Name, strm);
                                }
                            }
                        }
                        Scan(new DirectoryInfo(args[1]), "");
                        Console.WriteLine("Writing data...");
                        Stream outp = File.OpenWrite(args[2]);
                        builder.Write(outp);
                        outp.Close();
                        Console.WriteLine("Done!");
                        break;
                    }
                case "unpack":
                    {
                        if (!File.Exists(args[1]))
                        {
                            Console.Error.WriteLine("Error: source file does not exist");
                            Environment.Exit(1);
                        }
                        if (Directory.Exists(args[2]))
                        {
                            Console.Error.WriteLine("Warning: destination folder already exists!");
                            if (args.Length < 4 || args[3] != "confirm")
                            {
                                Console.Error.WriteLine("To continue anyways, add 'confirm' to the command.");
                                Environment.Exit(1);
                            }
                        }
                        Directory.CreateDirectory(args[2]);
                        Console.WriteLine("Extracting " + Path.GetFullPath(args[1]) + "...");
                        BinaryPackage pkg = new BinaryPackage(File.OpenRead(args[1]), args[1], () => File.OpenRead(args[1]));
                        foreach (BinaryPackageEntry ent in pkg.GetEntries())
                        {
                            Console.WriteLine("Extracting: " + ent.Key);
                            Stream strm = pkg.GetStream(ent);
                            Directory.CreateDirectory(Path.GetDirectoryName(args[2] + "/" + ent.Key));
                            Stream strmO = File.OpenWrite(args[2] + "/" + ent.Key);
                            strm.CopyTo(strmO);
                            strmO.Close();
                            strm.Close();
                        }
                        break;
                    }
            }
        }
    }
}