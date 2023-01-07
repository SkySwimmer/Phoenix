using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlayerLauncher
{
    public partial class Form1 : Form
    {
        private const string PX_SERVER = "http://localhost:8080";
        private string PlayerVendor;
        private string PlayerVersion;
        private string LaunchMethod;
        private string StartupArgsTemplate;
        private string PlayerFile;

        private string PlayerPath;

        private string ProductKey;
        private string DigitalSeal;
        private string Game;

        public Form1(string[] args)
        {
            Console.WriteLine("Phoenix Launcher Version 1.0");
            Console.WriteLine("Loading player details...");

            // Parse player details
            Dictionary<string, string> playerProps = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines("player/px.player.info"))
            {
                if (line == "")
                    continue;
                string key = line.Remove(line.IndexOf(": "));
                string value = line.Substring(line.IndexOf(": ") + 2);
                playerProps[key] = value;

                Console.WriteLine(key + " = " + value);
            }
            PlayerVendor = playerProps["Player-Vendor"];
            PlayerVersion = playerProps["Player-Version"];
            LaunchMethod = playerProps["Launch-Method"];
            StartupArgsTemplate = playerProps["Startup-Arguments"];
            PlayerFile = playerProps["Startup-File"];
            if (LaunchMethod != "handoff" && LaunchMethod != "arguments")
            {
                Console.Error.WriteLine("Corrupted player.");
                Environment.Exit(1);
            }

            Console.WriteLine("Processing arguments...");

            // Parse arguments
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
                        Console.WriteLine(key + " = " + value);
                    }
                    else
                    {
                        if (i + 1 < args.Length)
                        {
                            arguments[argS] = args[i + 1];
                            Console.WriteLine(argS + " = " + args[i + 1]);
                            i++;
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Startup failure!");
                            Console.Error.WriteLine("Invalid argument: " + arg + ": missing value.");
                            MessageBox.Show("Invalid argument: " + arg + ": missing value.", "Invalid usage", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Environment.Exit(0);
                        }
                    }
                }
            }

            // Check arguments
            if (!arguments.ContainsKey("gameid") || !arguments.ContainsKey("productkey") || !arguments.ContainsKey("digitalseal"))
            {
                Console.WriteLine();
                Console.WriteLine("Startup failure!");
                if (!arguments.ContainsKey("gameid"))
                    Console.Error.WriteLine("Missing argument: gameid");
                if (!arguments.ContainsKey("productkey"))
                    Console.Error.WriteLine("Missing argument: productkey");
                if (!arguments.ContainsKey("digitalseal"))
                    Console.Error.WriteLine("Missing argument: digitalseal");
                MessageBox.Show("No game specified at startup, please use a PX interface.", "Invalid usage", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            // Prepare folders
            Console.WriteLine("Preparing folders...");
            PlayerPath = "./data";
            if (arguments.ContainsKey("datafolder"))
                PlayerPath = arguments["datafolder"];
            Directory.CreateDirectory(PlayerPath);
            PlayerPath = Path.GetFullPath(PlayerPath);
            Directory.CreateDirectory(PlayerPath + "/assets");
            Directory.CreateDirectory(PlayerPath + "/gamedata");
            Directory.CreateDirectory(PlayerPath + "/playerdata");

            // Load arguments
            Game = arguments["gameid"];
            ProductKey = arguments["productkey"];
            DigitalSeal = arguments["digitalseal"];

            // Background work
            Thread th = new Thread(() =>
            {
                while (!Visible)
                {
                    Thread.Sleep(100);
                }

                Log("Finding package for game " + Game + "...");

                // Decode digital signature
                string payload = Encoding.UTF8.GetString(Base64Url.Decode(DigitalSeal));
                JObject[] segments = JsonConvert.DeserializeObject<JObject[]>(payload);
                DigitalSealPayload seal = segments[0].ToObject<DigitalSealPayload>();

                // Download game package
                string url = PX_SERVER + "/api/data/files/" + Game + "/" + seal.producthash + "/" + seal.timestamp + "/" + seal.gameid + ".game";
                string gameData = Download(url, "GET", headers: new Dictionary<string, string>()
                {
                    ["Product-Key"] = ProductKey,
                    ["Digital-Seal"] = DigitalSeal
                });

                // If download failed, check saved game data
                if (gameData == null && File.Exists(PlayerPath + "/gamedata/" + seal.gameid + ".game"))
                {
                    Console.Error.WriteLine("Failed to retrieve game startup package! Loading from memory!");

                    // Read from disk
                    gameData = File.ReadAllText(PlayerPath + "/gamedata/" + seal.gameid + ".game");
                }

                // Error
                if (gameData == null)
                {
                    Console.Error.WriteLine("Failed to retrieve game startup package!");
                    Invoke(new Action(() =>
                    {
                        MessageBox.Show("Could not authenticate request.\nPlease make sure you have a active internet connection and that the product key and digital seal are valid.", "Startup failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(1);
                    }));
                    return;
                }

                // Load details
                Console.WriteLine("Game package received!");
                Dictionary<string, string> game = new Dictionary<string, string>();
                foreach (string line in gameData.Split("\n"))
                {
                    if (line == "")
                        continue;
                    string key = line.Remove(line.IndexOf(": "));
                    string value = line.Substring(line.IndexOf(": ") + 2);
                    game[key] = value;

                    Console.WriteLine(key + " = " + value);
                }
                Log("Starting " + game["Game-Title"] + " " + game["Game-Version"] + "...");

                // Save details if the game supports offline play
                if (game["Offline-Support"] == "True")
                {
                    Console.WriteLine("Saving game package for offline play...");
                    string data = "";
                    foreach ((string key, string value) in game)
                    {
                        string v = value;
                        if (key == "Session")
                            v = "OFFLINE";
                        data += key + ": " + v + "\n";
                    }
                    File.WriteAllText(PlayerPath + "/gamedata/" + seal.gameid + ".game", data);
                    Console.WriteLine("Successfully saved game package.");
                }

                // Create arguments
                game["Assets-Path"] = PlayerPath + "/assets/" + seal.gameid + "/" + game["Game-Channel"] + "/" + game["Game-Version"] + "/" + game["Asset-Identifier"];
                game["Game-Storage-Path"] = PlayerPath + "/gamedata/" + seal.gameid + "/" + game["Game-Channel"] + "/" + game["Game-Version"];
                game["Player-Data-Path"] = PlayerPath + "/playerdata/" + seal.gameid;
                game["Data-Download-Base-Url"] = PX_SERVER + "/api/data/files/" + Game + "/" + seal.producthash + "/" + seal.timestamp;
                game["Product-Key"] = ProductKey;
                game["Digital-Seal"] = DigitalSeal;
                game["Refresh-Endpoint"] = PX_SERVER + "/api/tokens/refresh";
                string args = StartupArgsTemplate;
                Console.WriteLine("Building startup command...");
                TcpListener server = null;
                Random rnd = new Random();
                if (LaunchMethod == "arguments")
                {
                    foreach ((string key, string value) in game)
                    {
                        args = args.Replace("%" + key.ToLower() + "%", value);
                    }
                }
                else
                {
                    int port;
                    while (true)
                    {
                        try
                        {
                            port = rnd.Next(1024, short.MaxValue);
                            server = new TcpListener(IPAddress.Loopback, port);
                            server.Start();
                            break;
                        }
                        catch
                        {
                        }
                    }

                    args = args.Replace("%handoffport%", port.ToString());
                }

                // Prepare startup
                Directory.CreateDirectory(game["Assets-Path"]);
                Directory.CreateDirectory(game["Game-Storage-Path"]);
                Directory.CreateDirectory(game["Player-Data-Path"]);
                if (arguments.ContainsKey("debug-handoff") && Debugger.IsAttached)
                {
                    Log("Handoff Debug Mode Active");
                    Invoke(new Action(() => progressBar1.Style = ProgressBarStyle.Continuous));
                    server.Stop();
                    server = new TcpListener(IPAddress.Loopback, int.Parse(arguments["debug-handoff"]));
                    server.Start();

                    while (true)
                    {
                        TcpClient cl = server.AcceptTcpClient();
                        Console.WriteLine("Sending information to player...");
                        string data = "";
                        foreach ((string key, string value) in game)
                        {
                            data += key + ": " + value + "\n";
                        }
                        cl.GetStream().Write(Encoding.UTF8.GetBytes(data));
                        cl.Close();
                    }
                }
                else
                {
                    Console.WriteLine("Command: " + PlayerFile + " " + args);
                    Console.WriteLine("Working Directory: " + game["Player-Data-Path"]);
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.WorkingDirectory = game["Player-Data-Path"];
                    info.FileName = "player/" + PlayerFile;
                    info.Arguments = args;
                    info.UseShellExecute = false;
                    Process proc = Process.Start(info);

                    // Handoff
                    if (server != null && !proc.HasExited)
                    {
                        Console.WriteLine("Waiting for player to start...");
                        new Thread(() =>
                        {
                            while (!proc.HasExited)
                            {
                                Thread.Sleep(100);
                            }
                            server.Stop(); Environment.Exit(0);
                        }).Start();
                        TcpClient cl = server.AcceptTcpClient();
                        Console.WriteLine("Sending information to player...");
                        string data = "";
                        foreach ((string key, string value) in game)
                        {
                            data += key + ": " + value + "\n";
                        }
                        cl.GetStream().Write(Encoding.UTF8.GetBytes(data));
                    }
                    Environment.Exit(0);
                }
            });
            th.Name = "";
            th.Start();

            InitializeComponent();
            label2.Text = "Build: " + PlayerVersion + " (Vendor: " + PlayerVendor + ")";
        }

        string Download(string url, string method, string payload = null, Dictionary<string, string> headers = null)
        {
            // Log
            Console.WriteLine("Downloading: " + url + "...");

            Console.WriteLine("Request details:");
            Console.WriteLine("URL: " + url);
            Console.WriteLine("Method: " + method);
            if (headers != null)
            {
                foreach ((string key, string value) in headers)
                {
                    Console.WriteLine("[" + key + "]: " + value);
                }
            }

            // Download
            try
            {
                HttpClient cl = new HttpClient();
                if (headers != null)
                    foreach ((string key, string value) in headers)
                    {
                        cl.DefaultRequestHeaders.Add(key, value);
                    }
                if (method == "GET")
                    return cl.GetStringAsync(url).GetAwaiter().GetResult();
                else if (method == "POST")
                    return cl.PostAsync(url, new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                else
                    throw new ArgumentException("method");
            }
            catch (Exception e)
            {
                // Failure
                Console.Error.WriteLine("Download failure!");
                Console.Error.WriteLine(e);
                return null;
            }
        }

        void Log(string message)
        {
            Invoke(new Action(() =>
            {
                label3.Text = message;
                label3.Update();
                Console.WriteLine(message);
            }));
        }
    }
}
