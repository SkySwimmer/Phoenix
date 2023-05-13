using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PGL_Launcher
{
    public static class Program
    {
        /// <summary>
        /// API endpoint
        /// </summary>
        public static string PX_SERVER = "https://aerialworks.ddns.net/api/";

        /// <summary>
        /// Game data path
        /// </summary>
        public static string GAME_DIRECTORY = "";

        /// <summary>
        /// Game property map
        /// </summary>
        public static Dictionary<string, string> GameProperties = new Dictionary<string, string>();

        /// <summary>
        /// Banner image path
        /// </summary>
        public static string BANNER_IMAGE;

        /// <summary>
        /// Digital seal
        /// </summary>
        public static DigitalSealPayload DIGITAL_SEAL;

        /// <summary>
        /// Installation information file path
        /// </summary>
        public static string INSTALL_INFO_FILE;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Read game information
            StreamReader rd = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("PGL_Launcher.game.info"));
            while (!rd.EndOfStream)
            {
                string l = rd.ReadLine();
                if (!l.Contains("="))
                    continue;
                GameProperties[l.Remove(l.IndexOf("="))] = l.Substring(l.IndexOf("=") + 1);
            }
            rd.Close();

            // Editor mode if the game file is empty
            if (GameProperties.Count == 0)
            {
                // Check command line
                if (args.Length == 2 && args[0].EndsWith(".info") && File.Exists(args[0]))
                {
                    // Create launcher
                    if (!CreateLauncher(File.ReadAllText(args[0]), args[1]))
                        Environment.Exit(1);
                    return;
                }

                // Preparation
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Open editor
                Application.Run(new LauncherEditor());
                return;
            }

            // Set server if present
            if (GameProperties.ContainsKey("PhoenixServer"))
            {
                PX_SERVER = GameProperties["PhoenixServer"];
                if (!PX_SERVER.EndsWith("/"))
                    PX_SERVER += "/";
            }

            // Preparation
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load or create the local game directory
            GAME_DIRECTORY = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/" + GameProperties["GameID"];
            Directory.CreateDirectory(GAME_DIRECTORY);

            // Attempt to download the banner
            try
            {
                HttpClient client = new HttpClient();
                Stream file = client.GetStreamAsync(GameProperties["Banner"]).GetAwaiter().GetResult();
                FileStream output = File.OpenWrite(GAME_DIRECTORY + "/banner");
                file.CopyTo(output);
                output.Close();
                file.Close();
            }
            catch
            {
            }

            // Check if the banner file exists
            if (!File.Exists(GAME_DIRECTORY + "/banner"))
            {
                // Prompt the user to connect to the internet
                MessageBox.Show("Please connect to the internet before proceeding.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }
            BANNER_IMAGE = GAME_DIRECTORY + "/banner";

            // Check if there is a installation properties file
            INSTALL_INFO_FILE = GAME_DIRECTORY + "/installation.info";
            if (File.Exists(INSTALL_INFO_FILE))
            {
                // Read the file
                rd = new StreamReader(INSTALL_INFO_FILE);
                while (!rd.EndOfStream)
                {
                    string l = rd.ReadLine();
                    if (!l.Contains("="))
                        continue;
                    if (!GameProperties.ContainsKey(l.Remove(l.IndexOf("="))))
                        GameProperties[l.Remove(l.IndexOf("="))] = l.Substring(l.IndexOf("=") + 1);
                }
                rd.Close();
            }

            // Find installation directory
            if (GameProperties.ContainsKey("InstallationDir"))
                GAME_DIRECTORY = GameProperties["InstallationDir"];
            else
                GAME_DIRECTORY = GAME_DIRECTORY + "/files";

            // Decode digital signature
            string payload = Encoding.UTF8.GetString(Base64Url.Decode(GameProperties["DigitalSeal"]));
            JObject[] segments = JsonConvert.DeserializeObject<JObject[]>(payload);
            DIGITAL_SEAL = segments[0].ToObject<DigitalSealPayload>();

            // Open the launcher
            Directory.CreateDirectory(GAME_DIRECTORY);
            Application.Run(new LauncherWindow());
        }

        public static bool CreateLauncher(string doc, string output)
        {
            // Log
            Console.WriteLine("Creating launcher...");
            Console.WriteLine("Output folder: " + output);
            Console.WriteLine("Finding project files...");

            string projFiles = "project";
            if (!File.Exists(projFiles + "/PGL Launcher.sln"))
            {
                projFiles = ".";
            }
            if (!File.Exists(projFiles + "/PGL Launcher.sln"))
            {
                projFiles = "..";
            }
            if (!File.Exists(projFiles + "/PGL Launcher.sln"))
            {
                projFiles = "../..";
            }
            if (!File.Exists(projFiles + "/PGL Launcher.sln"))
            {
                projFiles = "../../..";
            }
            if (!File.Exists(projFiles + "/PGL Launcher.sln"))
            {
                Console.Error.WriteLine("Unable to locate the launcher project files!");
                try
                {
                    MessageBox.Show("Could not locate the launcher C# project files, please download the launcher source to a folder named 'project' in the current directoy.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {}
                return false;
            }

            // Log
            Console.WriteLine("Project directory: " + Path.GetFullPath(projFiles));

            // Copy project files
            Console.WriteLine("Copying project directory...");
            IOUtils.DeleteDirectory("tmp");
            IOUtils.CopyDirectory(projFiles, "tmp", Environment.CurrentDirectory);
            IOUtils.DeleteDirectory("tmp/PGL Launcher/bin");
            IOUtils.DeleteDirectory("tmp/PGL Launcher/obj");

            // Write game.info
            Console.WriteLine("Writing game.info...");
            File.WriteAllText("tmp/PGL Launcher/game.info", doc);

            // Compile laucher
            Console.WriteLine("Compiling launcher...");

            // Locate MSBUILD
            string cmd = "msbuild";
            // Locate VS msbuild
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "/Microsoft Visual Studio/Installer/vswhere";
                info.Arguments = "-latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\\**\\MSBuild.exe";
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.RedirectStandardInput = true;
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                Process proc = Process.Start(info);
                string o = proc.StandardOutput.ReadToEnd().Replace("\r", "");
                proc.WaitForExit();
                cmd = o.Split('\n')[0];
            }
            catch
            {
            }
            if (cmd == "msbuild")
            {
                try
                {
                    string path = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions\\4.0", "MSBuildToolsPath", "").ToString();
                    if (path != "")
                        cmd = path + "/msbuild";
                }
                catch
                {
                }
            }

            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "nuget";
                info.Arguments = "restore";
                info.WorkingDirectory = Path.GetFullPath("tmp");
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                Process proc = Process.Start(info);
                proc.WaitForExit();
            }
            catch
            {
            }
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = cmd;
                info.Arguments = "\"PGL Launcher/PGL Launcher.csproj\" -property:Configuration=RELEASE";
                info.WorkingDirectory = Path.GetFullPath("tmp");
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                Process proc = Process.Start(info);
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    throw new Exception();
                }
            }
            catch
            {
                Console.Error.WriteLine("Compilation failure!");
                try
                {
                    MessageBox.Show("Could not compile the launcher!\nPlease make sure you have the .NET SDK installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                { }
                return false;
            }

            // Copy result
            Console.WriteLine("Copying files...");
            Directory.CreateDirectory(output);
            foreach (FileInfo file in new DirectoryInfo("tmp/PGL Launcher/bin/Release").GetFiles())
            {
                if (!file.Name.EndsWith(".xml") && !file.Name.EndsWith(".config") && !file.Name.EndsWith(".pdb") && !file.Name.EndsWith(".json"))
                {
                    file.CopyTo(output + "/" + file.Name, true);
                }
            }

            // Cleanup
            Console.WriteLine("Cleaning up...");
            IOUtils.DeleteDirectory("tmp");

            return true;
        }
    }
}
