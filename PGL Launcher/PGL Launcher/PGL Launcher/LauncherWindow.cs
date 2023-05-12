using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PGL_Launcher
{
    public partial class LauncherWindow : Form
    {
        public LauncherWindow()
        {
            InitializeComponent();

            // Load image into background image
            try
            {
                BackgroundImage = Image.FromFile(Program.BANNER_IMAGE);
            }
            catch
            {
                MessageBox.Show("Failed to load the launcher, files corrupted, please connect to the internet to re-download.\n\nIf you are connected to the internet and this issue persists, please contact the support team of the game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

            // Check if the product key is present
            Program.GameProperties["ProductKey"] = null;
            if (File.Exists(Program.GAME_DIRECTORY + "/product.key"))
            {
                // Load product key
                try
                {
                    Program.GameProperties["ProductKey"] = Encoding.UTF8.GetString(
                        AesUtil.Decrypt(
                            File.ReadAllBytes(Program.GAME_DIRECTORY + "/product.key"),
                            Encoding.UTF8.GetBytes(Program.GameProperties["ProductEncryption"])
                        ));
                }
                catch { }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void LauncherWindow_Shown(object sender, EventArgs e)
        {
            while (true)
            {
                // Check if the product key is present
                if (Program.GameProperties["ProductKey"] == null)
                {
                    ProductKeyInput inp = new ProductKeyInput();
                    inp.ShowDialog();
                    if (inp.Result == null)
                        Environment.Exit(0);
                    Program.GameProperties["ProductKey"] = inp.Result.ToUpper();
                }

                // Check product key
                using (var sha512 = new SHA512CryptoServiceProvider())
                {
                    string hash = string.Concat(sha512.ComputeHash(Encoding.UTF8.GetBytes(Program.GameProperties["ProductKey"])).Select(x => x.ToString("x2")));
                    if (!hash.Equals(Program.DIGITAL_SEAL.producthash))
                    {
                        // Invalid product key
                        MessageBox.Show("Invalid product key, please verify your key and try again.", "Invalid key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Program.GameProperties["ProductKey"] = null;
                    }
                    else
                    {
                        // Save and continue
                        File.WriteAllBytes(Program.GAME_DIRECTORY + "/product.key", AesUtil.Encrypt(Encoding.UTF8.GetBytes(Program.GameProperties["ProductKey"]), Encoding.UTF8.GetBytes(Program.GameProperties["ProductEncryption"])));
                        break;
                    }
                }
            }

            // Retrieve game information document
            string url = Program.PX_SERVER + "/api/data/files/" + Program.GameProperties["GameID"] + "/" + Program.DIGITAL_SEAL.producthash + "/" + Program.DIGITAL_SEAL.timestamp;

            // Attempt to download the game document
            bool connected = true;
            Dictionary<string, string> game = new Dictionary<string, string>();
            string gameDoc = null;
            void downloadGameDoc()
            {
                gameDoc = DownloadUtils.DownloadString(url + "/" + Program.DIGITAL_SEAL.gameid + ".game", "GET", headers: new Dictionary<string, string>()
                {
                    ["Digital-Seal"] = Program.GameProperties["DigitalSeal"],
                    ["Product-Key"] = Program.GameProperties["ProductKey"]
                });
                if (gameDoc == null)
                {
                    connected = false;

                    // Find saved document
                    if (File.Exists(Program.GAME_DIRECTORY + "/game.info"))
                    {
                        gameDoc = File.ReadAllText(Program.GAME_DIRECTORY + "/game.info");
                    }
                }
                if (gameDoc != null)
                {
                    // Parse game document
                    foreach (string line in gameDoc.Replace("\r", "").Split('\n'))
                    {
                        if (!line.StartsWith("#") && line.Contains(": "))
                        {
                            string key = line.Remove(line.IndexOf(": "));
                            string value = line.Substring(line.IndexOf(": ") + 2);
                            game[key] = value;
                        }
                    }

                    // Attempt to find version-specific data
                    try
                    {
                        if (Program.GameProperties.ContainsKey("GameVersion"))
                        {
                            string gameDocVerSpecific = DownloadUtils.DownloadString(url + "/" + Program.DIGITAL_SEAL.gameid + "-" + Program.GameProperties["GameVersion"] + ".game", "GET", headers: new Dictionary<string, string>()
                            {
                                ["Digital-Seal"] = Program.GameProperties["DigitalSeal"],
                                ["Product-Key"] = Program.GameProperties["ProductKey"]
                            });

                            if (gameDocVerSpecific != null)
                            {
                                // Parse game document
                                foreach (string line in gameDocVerSpecific.Replace("\r", "").Split('\n'))
                                {
                                    if (!line.StartsWith("#") && line.Contains(": "))
                                    {
                                        string key = line.Remove(line.IndexOf(": "));
                                        string value = line.Substring(line.IndexOf(": ") + 2);
                                        game[key] = value;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {}

                    // Save document (without tokens)
                    string doc = "";
                    foreach (string key in game.Keys)
                    {
                        string value = game[key];
                        if (key == "Session")
                            value = "OFFLINE";
                        doc += key + ": " + value + "\n";
                    }
                    File.WriteAllText(Program.GAME_DIRECTORY + "/game.info", doc);
                }
                else
                {
                    // Show error
                    MessageBox.Show("Could not download game details, please verify your internet connection and try again.\n\nIf your internet is connected and the error persists, please contact support.", "Failed to load game details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Environment.Exit(1);
                    return;
                }
            }
            downloadGameDoc();

            // Check if the game supports offline play
            if (!connected)
            {
                if (game["Offline-Support"] != "True")
                {
                    MessageBox.Show("Please connect your device to the internet.", "No connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                    return;
                }
            }

            // Check game descriptor
            string platform = Environment.OSVersion.Platform == PlatformID.Unix ? "linux" : (Environment.OSVersion.Platform == PlatformID.MacOSX ? "osx" : "windows");
            if (!game.ContainsKey("Game-Title") || !game.ContainsKey("Game-Version") || !game.ContainsKey("ToS-Version") || !game.ContainsKey("ToS-File")
                 || !game.ContainsKey("Game-Channel") || !game.ContainsKey("Asset-Identifier") || !game.ContainsKey("Game-Files-Endpoint")
                 || !game.ContainsKey("Game-Executable-" + (platform == "windows" ? "Win64" : (platform == "linux" ? "Linux" : "OSX")))) {
                MessageBox.Show("Failed to prepare the launcher!\n\nPlease contact support and give them the following:\n" +
                    "One or more required game descriptor fields are not present in the server-side game descriptor document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

#if !DEBUG
            try
            {
#endif
                // Set window title
                Text = game["Game-Title"] + " - Game Launcher";

                // Check last saved game version
                string currentVersion = game["Game-Version"];
                if (Program.GameProperties.ContainsKey("GameVersion"))
                    currentVersion = Program.GameProperties["GameVersion"];
                if (Program.GameProperties.ContainsKey("LastGameVersion"))
                {
                    string lastVersion = Program.GameProperties["LastGameVersion"];

                    // Check if its the same
                    if (lastVersion != currentVersion)
                    {
                        // Warn the user
                        if (MessageBox.Show("The major version of " + game["Game-Title"] + " has changed!\n" +
                            "This often means you have upgraded or downgraded the launcher, please verify below.\n" +
                            "\nCurrent installed version: " + lastVersion +
                            "\nNew version: " + currentVersion + "\n" +
                            "\nAre you sure you want to continue?", "Major update version changed", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        {
                            Environment.Exit(1);
                            return;
                        }
                    }
                }

                // Check ToS version
                string lastToSVersion = null;
                if (Program.GameProperties.ContainsKey("TermsVersion"))
                    lastToSVersion = Program.GameProperties["TermsVersion"];
                bool isNewInstall = lastToSVersion == null;
                if (lastToSVersion != game["ToS-Version"])
                {
                    // Terms of service changed or new installation
                    if (!isNewInstall)
                    {
                        // Inform the player the terms have changed
                        MessageBox.Show("The Terms of Service have changed, press OK to view them.", "Terms of Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    // Load terms
                    string termsText = DownloadUtils.DownloadString(game["ToS-File"], "GET");
                    if (termsText == null)
                    {
                        MessageBox.Show("Failed to download the Terms of Service.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(1);
                        return;
                    }

                    // Show ToS window
                    TermsWindow terms = new TermsWindow(game["Game-Title"], termsText);
                    terms.ShowDialog();
                    if (!terms.Accepted)
                    {
                        Environment.Exit(1);
                        return;
                    }
                }

                // Installation folder selection
                if (isNewInstall)
                {
                    while (true)
                    {
                        if (MessageBox.Show("Do you want to use the default installation directory?", "Installation directory", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            // Select installation directory
                            FolderBrowserDialog dialog = new FolderBrowserDialog();
                            dialog.Description = "Select game installation directory...";
                            if (dialog.ShowDialog() != DialogResult.OK || dialog.SelectedPath == null)
                            {
                                // No selection
                                continue;
                            }

                            if (Directory.Exists(dialog.SelectedPath + "/" + game["Game-Title"]))
                            {
                                // Warn the user
                                MessageBox.Show("A folder named '" + Program.GameProperties["Game-Title"] + "' already exists in the selected installation directory!", "Directory exists!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                continue;
                            }

                            // Move files
                            Directory.CreateDirectory(dialog.SelectedPath);
                            IOUtils.CopyDirectory(Program.GAME_DIRECTORY, dialog.SelectedPath + "/" + game["Game-Title"]);
                            IOUtils.DeleteDirectory(Program.GAME_DIRECTORY);
                            Program.GAME_DIRECTORY = Path.GetFullPath(dialog.SelectedPath + "/" + game["Game-Title"]);
                        }

                        break;
                    }
                }

                // Save installation information file
                if (Program.GameProperties.ContainsKey("LastGameVersion"))
                {
                    StreamWriter wr = new StreamWriter(Program.INSTALL_INFO_FILE);
                    wr.WriteLine("TermsVersion=" + game["ToS-Version"]);
                    wr.WriteLine("InstallationDir=" + Program.GAME_DIRECTORY);
                    wr.WriteLine("LastGameVersion=" + currentVersion);
                    wr.Close();
                }

                // Update check
                Thread th = new Thread(() =>
                {
                    if (!isNewInstall)
                    {
                        Invoke(new Action(() =>
                        {
                            label1.Text = "Resolving updates...";
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() =>
                        {
                            label1.Text = "Resolving game files...";
                        }));
                    }

                    // Find current build version
                    string versionInfo = DownloadUtils.DownloadString(game["Game-Files-Endpoint"] + "/" + platform + "/versions/" + currentVersion + ".json", "GET", null, new Dictionary<string, string>()
                    {
                        ["Digital-Seal"] = Program.GameProperties["DigitalSeal"],
                        ["Product-Key"] = Program.GameProperties["ProductKey"]
                    });
                    bool updateNeeded = !Program.GameProperties.ContainsKey("LastGameVersion") || currentVersion != Program.GameProperties["LastGameVersion"];
                    if (versionInfo == null)
                    {
                        // Download failure, check if the game is already installed
                        if (!Program.GameProperties.ContainsKey("LastGameVersion") || updateNeeded)
                        {
                            if (connected)
                            {
                                // Download error
                                MessageBox.Show("Failed to resolve the game file manifest!\nPlease contact the support team for this game!\n\nDetailed error:\nFile /" + platform + "/versions/" + currentVersion + ".json could not be found on server.", "Download failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Environment.Exit(1);
                                return;
                            }
                            else
                            {
                                // No internet
                                MessageBox.Show("Please connect your device to the internet.", "No connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Environment.Exit(1);
                                return;
                            }
                        }
                    }

                    // Update check
                    if (versionInfo != null)
                    {
                        // Download success, compare versions and compile a list of files to download
                        Dictionary<string, string> files = new Dictionary<string, string>();
                        GameVersionManifest manifest = JsonConvert.DeserializeObject<GameVersionManifest>(versionInfo);
                        foreach (string key in manifest.changedFiles.Keys)
                        {
                            if (!files.ContainsKey(key))
                                files[key] = manifest.changedFiles[key];
                        }
                        GameVersionManifest cman = manifest;
                        while (cman.previousVersion != null || Program.GameProperties.ContainsKey("LastGameVersion") && cman.previousVersion == Program.GameProperties["LastGameVersion"])
                        {
                            // Find all other versions we need to get the files from
                            string newDoc = DownloadUtils.DownloadString(game["Game-Files-Endpoint"] + "/" + platform + "/versions/" + cman.previousVersion + ".json", "GET", null, new Dictionary<string, string>()
                            {
                                ["Digital-Seal"] = Program.GameProperties["DigitalSeal"],
                                ["Product-Key"] = Program.GameProperties["ProductKey"]
                            });
                            if (newDoc == null)
                                break;
                            cman = JsonConvert.DeserializeObject<GameVersionManifest>(newDoc);
                            foreach (string key in cman.changedFiles.Keys)
                            {
                                if (!files.ContainsKey(key))
                                    files[key] = cman.changedFiles[key];
                            }
                        }


                        // File verification
                        Dictionary<string, string> localFiles = new Dictionary<string, string>();

                        // Log
                        Invoke(new Action(() =>
                        {
                            label1.Text = "Verifying game files...";
                        }));

                        // Load local manifest
                        if (File.Exists(Program.GAME_DIRECTORY + "/installation.files.info"))
                        {
                            // Find all files (ignore those missing on disk)
                            foreach (string line in File.ReadAllLines(Program.GAME_DIRECTORY + "/installation.files.info"))
                            {
                                if (!line.Contains("="))
                                    continue;
                                string key = line.Remove(line.LastIndexOf("="));
                                string value = line.Substring(line.LastIndexOf("=") + 1);
                                if (File.Exists(Program.GAME_DIRECTORY + "/gamefiles/" + key))
                                {
                                    // File is saved
                                    localFiles[key] = value;
                                }
                            }
                        }

                        // Loop through update files
                        bool changed = false;
                        int val = 0;
                        foreach (string file in files.Keys)
                        {
                            if (files[file] == "DELETE")
                            {
                                // Delete
                                if (localFiles.ContainsKey(file))
                                {
                                    if (!changed)
                                    {
                                        // Log and set progress bar
                                        Invoke(new Action(() =>
                                        {
                                            label1.Text = "Updating game files...";
                                            progressBar1.Maximum = files.Count * 100;
                                            progressBar1.Value = val;
                                            progressBar1.Style = ProgressBarStyle.Continuous;
                                        }));
                                        changed = true;
                                    }
                                    localFiles.Remove(file);
                                    if (File.Exists(Program.GAME_DIRECTORY + "/gamefiles/" + file))
                                        File.Delete(Program.GAME_DIRECTORY + "/gamefiles/" + file);
                                    // Set progress bar
                                    Invoke(new Action(() =>
                                    {
                                        val += 100;
                                        progressBar1.Value = val;
                                    }));
                                }
                                else
                                {
                                    val += 100;
                                    if (changed)
                                    {
                                        // Set progress bar
                                        Invoke(new Action(() =>
                                        {
                                            progressBar1.Value = val;
                                        }));
                                    }
                                }
                            }
                            else
                            {
                                // Update or download
                                if (!localFiles.ContainsKey(file) || localFiles[file] != files[file] || !File.Exists(Program.GAME_DIRECTORY + "/gamefiles/" + file))
                                {
                                    if (!changed)
                                    {
                                        // Log and set progress bar
                                        Invoke(new Action(() =>
                                        {
                                            label1.Text = "Updating game files...";
                                            progressBar1.Maximum = files.Count * 100;
                                            progressBar1.Value = val;
                                            progressBar1.Style = ProgressBarStyle.Continuous;
                                        }));
                                        changed = true;
                                    }

                                    // Download
                                    long max;
                                    Stream strm = DownloadUtils.Download(game["Game-Files-Endpoint"] + "/downloads/" + files[file], "GET", out max, null, new Dictionary<string, string>()
                                    {
                                        ["Digital-Seal"] = Program.GameProperties["DigitalSeal"],
                                        ["Product-Key"] = Program.GameProperties["ProductKey"]
                                    });
                                    if (strm == null)
                                    {
                                        // Failure
                                        MessageBox.Show("Failed to download game files, current progress has been saved, please try again.", "Download failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        Environment.Exit(1);
                                        return;
                                    }
                                    double step = 100d / max;
                                    long pos = 0;
                                    int cval = val;
                                    Directory.CreateDirectory(Path.GetDirectoryName(Program.GAME_DIRECTORY + "/gamefiles/" + file));
                                    Stream strmO = File.OpenWrite(Program.GAME_DIRECTORY + "/gamefiles/" + file);
                                    while (pos < max)
                                    {
                                        try
                                        {
                                            byte[] buffer = new byte[(max - pos) > 5000 ? 5000 : (max - pos)];
                                            int l = strm.Read(buffer, 0, buffer.Length);
                                            if (l <= 0)
                                            {
                                                // Failure
                                                MessageBox.Show("Failed to download game files, current progress has been saved, please try again.", "Download failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                Environment.Exit(1);
                                                return;
                                            }

                                            strmO.Write(buffer, 0, l);

                                            // Update progress
                                            val = cval + (int)(step * (pos + l));
                                            Invoke(new Action(() =>
                                            {
                                                progressBar1.Value = val;
                                            }));
                                            pos += l;
                                        }
                                        catch
                                        {
                                            // Failure
                                            MessageBox.Show("Failed to download game files, current progress has been saved, please try again.", "Download failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            Environment.Exit(1);
                                            return;
                                        }
                                    }
                                    strmO.Close();
                                    pos = max;
                                    val = cval + 100;
                                    Invoke(new Action(() =>
                                    {
                                        progressBar1.Value = val;
                                    }));

                                    // Set value
                                    localFiles[file] = files[file];
                                    strm.Close();
                                }
                            }

                            if (changed)
                                saveManifest();
                        }

                        void saveManifest()
                        {
                            // Save manifest
                            StreamWriter writer = new StreamWriter(Program.GAME_DIRECTORY + "/installation.files.info");
                            foreach (string key in localFiles.Keys)
                                writer.WriteLine(key + "=" + localFiles[key]);
                            writer.Close();

                            // Save installation information file
                            StreamWriter wr = new StreamWriter(Program.INSTALL_INFO_FILE);
                            wr.WriteLine("TermsVersion=" + game["ToS-Version"]);
                            wr.WriteLine("InstallationDir=" + Program.GAME_DIRECTORY);
                            wr.WriteLine("LastGameVersion=" + currentVersion);
                            wr.Close();
                        }

                        if (changed)
                        {
                            Invoke(new Action(() =>
                            {
                                progressBar1.Value = progressBar1.Maximum;
                            }));
                            Thread.Sleep(1000);
                            Invoke(new Action(() =>
                            {
                                progressBar1.Maximum = 100;
                                progressBar1.Value = 0;
                                progressBar1.Style = ProgressBarStyle.Marquee;
                            }));
                        }
                    }

                    // Log
                    Invoke(new Action(() =>
                    {
                        label1.Text = "Retrieving game token...";
                    }));

                    // Retrieve new game document
                    downloadGameDoc();

                    // Log
                    Invoke(new Action(() =>
                    {
                        label1.Text = "Preparing to start the game...";
                    }));

                    // Assign properties
                    game["Assets-Path"] = Program.GAME_DIRECTORY + "/assets/" + game["Game-Channel"] + "/" + game["Game-Version"] + "/" + game["Asset-Identifier"];
                    game["Game-Storage-Path"] = Program.GAME_DIRECTORY + "/gamefiles";
                    game["Player-Data-Path"] = Program.GAME_DIRECTORY + "/playerdata";
                    game["Save-Data-Path"] = Program.GAME_DIRECTORY + "/savedata";
                    game["Data-Download-Base-Url"] = Program.PX_SERVER + "/api/data/files/" + Program.DIGITAL_SEAL.gameid + "/" + Program.DIGITAL_SEAL.producthash + "/" + Program.DIGITAL_SEAL.timestamp;
                    game["Product-Key"] = Program.GameProperties["ProductKey"];
                    game["Digital-Seal"] = Program.GameProperties["DigitalSeal"];
                    game["Refresh-Endpoint"] = Program.PX_SERVER + "/api/tokens/refresh";

                    // Start a TCP server for data handoff
                    Random rnd = new Random();
                    TcpListener server = null;
                    int port;
                    while (true)
                    {
                        try
                        {
                            port = rnd.Next(1024, short.MaxValue);
                            server = new TcpListener(System.Net.IPAddress.Loopback, port);
                            server.Start();
                            break;
                        }
                        catch
                        {
                        }
                    }

                    // Create folders
                    Directory.CreateDirectory(game["Assets-Path"]);
                    Directory.CreateDirectory(game["Game-Storage-Path"]);
                    Directory.CreateDirectory(game["Player-Data-Path"]);
                    Directory.CreateDirectory(game["Save-Data-Path"]);

                    // Log
                    Invoke(new Action(() =>
                    {
                        label1.Text = "Starting game...";
                    }));

                    // Build process info
                    ProcessStartInfo info = new ProcessStartInfo();
                    if (platform == "linux")
                    {
                        ProcessStartInfo chmod = new ProcessStartInfo();
                        chmod.FileName = "chmod";
                        chmod.Arguments = "+x '" + game["Game-Storage-Path"] + "/" + (platform == "windows" ? game["Game-Executable-Win64"] : (platform == "linux" ? game["Game-Executable-Linux"] : game["Game-Executable-OSX"])) + "'";
                        Process.Start(chmod).WaitForExit();
                    }
                    string argsProp = (platform == "windows" ? "Game-Arguments-Win64" : (platform == "linux" ? "Game-Arguments-Linux" : "Game-Arguments-OSX"));
                    info.FileName = game["Game-Storage-Path"] + "/" + (platform == "windows" ? game["Game-Executable-Win64"] : (platform == "linux" ? game["Game-Executable-Linux"] : game["Game-Executable-OSX"]));
                    info.WorkingDirectory = game["Player-Data-Path"];
                    info.Arguments = "--play --gamedoc " + port + (game.ContainsKey(argsProp) ? " " + game[argsProp] : "");
                    info.UseShellExecute = false;
                    Process proc = Process.Start(info);

                    // Log
                    Invoke(new Action(() =>
                    {
                        label1.Text = "Waiting for game startup...";
                    }));

                    // Exit thread
                    new Thread(() =>
                    {
                        while (!proc.HasExited)
                        {
                            Thread.Sleep(100);
                        }
                        server.Stop();
                        Environment.Exit(0);
                    }).Start();

                    // Data transmission
                    TcpClient cl = server.AcceptTcpClient();
                    string data = "";
                    foreach (string key in game.Keys)
                    {
                        data += key + ": " + game[key] + "\n";
                    }
                    byte[] payload = Encoding.UTF8.GetBytes(data);
                    cl.GetStream().Write(payload, 0, payload.Length);
                    cl.Close();
                    Environment.Exit(0);
                });
                th.IsBackground = true;
                th.Start();
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to prepare the launcher!\n\nPlease contact support and give them the following:\n" +
                    "Exception: " + ex.GetType().Name + (ex.Message != null ? ": " + ex.Message : ""), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }
#endif
        }
    }
}
