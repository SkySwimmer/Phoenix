using System.Globalization;
using System.IO;
using Phoenix.Common.IO;

namespace Phoenix.Debug.BinManifestTool
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("pbmanutil \"<file>\" \"<output file>\"");
                Environment.Exit(1);
            }

            // Check
            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine("Error: source file does not exist");
                Environment.Exit(1);
            }
            if (File.Exists(args[1]))
            {
                Console.Error.WriteLine("Warning: destination file already exists! It will be overwritten!");
                if (args.Length < 3 || args[2] != "confirm")
                {
                    Console.Error.WriteLine("To continue anyways, add 'confirm' to the command.");
                    Environment.Exit(1);
                }
            }

            // Prepare writer
            Stream outp = File.OpenWrite(args[1]);
            DataWriter writer = new DataWriter(outp);
            foreach (string line in File.ReadLines(args[0]))
            {
                if (line == "" || line.StartsWith("#"))
                    continue;
                string type = "STRING";
                string data = line;
                if (data.StartsWith("!"))
                {
                    data = data.Substring(1);
                    if (!data.Contains("!"))
                    {
                        Console.Error.WriteLine("Error: unterminated data type tag: " + data);
                        outp.Close();
                        Environment.Exit(1);
                        return;
                    }
                    type = data.Remove(data.IndexOf("!"));
                    data = data.Substring(data.IndexOf("!") + 1);
                }

                try
                {
                    switch (type)
                    {
                        case "STRING":
                            {
                                writer.WriteString(data);
                                break;
                            }
                        case "INT16":
                            {
                                writer.WriteShort(short.Parse(data));
                                break;
                            }
                        case "INT32":
                            {
                                writer.WriteInt(int.Parse(data));
                                break;
                            }
                        case "INT64":
                            {
                                writer.WriteLong(long.Parse(data));
                                break;
                            }
                        case "FLOAT":
                            {
                                writer.WriteFloat(float.Parse(data));
                                break;
                            }
                        case "DOUBLE":
                            {
                                writer.WriteDouble(double.Parse(data));
                                break;
                            }
                        case "BOOLEAN":
                            {
                                writer.WriteBoolean(data.ToLower() == "true");
                                break;
                            }
                        case "BYTE":
                            {
                                writer.WriteRawByte(byte.Parse(data));
                                break;
                            }
                        case "BYTES":
                            {
                                writer.WriteBytes(Convert.FromBase64String(data));
                                break;
                            }
                        case "RAW":
                            {
                                writer.WriteRawBytes(Convert.FromBase64String(data));
                                break;
                            }
                        default:
                            {
                                Console.Error.WriteLine("Error: invalid data type tag: " + type);
                                outp.Close();
                                Environment.Exit(1);
                                break;
                            }
                    }
                }
                catch
                {
                    Console.Error.WriteLine("Error: invalid value for type " + type + ": " + data);
                    outp.Close();
                    Environment.Exit(1);
                    break;
                }
            }
            outp.Close();
        }
    }
}