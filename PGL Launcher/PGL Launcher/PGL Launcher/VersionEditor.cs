using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PGL_Launcher
{
    public partial class VersionEditor : Form
    {
        public string currentVersion = null;
        public Dictionary<string, GameVersionManifest> winVersions = new Dictionary<string, GameVersionManifest>();
        public Dictionary<string, GameVersionManifest> linuxVersions = new Dictionary<string, GameVersionManifest>();
        public Dictionary<string, GameVersionManifest> osxVersions = new Dictionary<string, GameVersionManifest>();
        public string path;

        public VersionEditor()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            button1.Enabled = false;

            richTextBox1.Text = "";
            richTextBox1.AppendText("Processing files...");
            string inheritedVersion = null;
            if (winVersions.ContainsKey(currentVersion))
                inheritedVersion = winVersions[currentVersion].previousVersion;
            else if (linuxVersions.ContainsKey(currentVersion))
                inheritedVersion = linuxVersions[currentVersion].previousVersion;
            else if (osxVersions.ContainsKey(currentVersion))
                inheritedVersion = osxVersions[currentVersion].previousVersion;
            else
                throw new InvalidOperationException("The fuck-");

            if (textBox2.Text == "" && textBox3.Text == "" && textBox4.Text == "")
            {
                richTextBox1.AppendText("\nError: missing update files,\nplease select files for at least ONE platform.");
                button2.Enabled = true;
                button1.Enabled = true;
                return;
            }

            new Thread(() =>
            {
                if (textBox2.Text != "" && Directory.Exists(textBox2.Text))
                {
                    // Process windows update files
                    Process("windows", textBox2.Text);
                }
                if (textBox3.Text != "" && Directory.Exists(textBox3.Text))
                {
                    // Process linux update files
                    Process("linux", textBox3.Text);
                }
                if (textBox4.Text != "" && Directory.Exists(textBox4.Text))
                {
                    // Process osx update files
                    Process("osx", textBox4.Text);
                }

                // Log
                Invoke(new Action(() =>
                {
                    richTextBox1.AppendText("\nCompleted!");
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                    MessageBox.Show("Version '" + textBox1.Text + "' has been successfully updated!");
                    Close();
                }));

                void Process(string plat, string input)
                {
                    // Log
                    Invoke(new Action(() =>
                    {
                        richTextBox1.AppendText("\nProcessing " + plat + " files...");
                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                        richTextBox1.ScrollToCaret();
                    }));

                    // Create file map
                    Dictionary<string, string> knownHashes = new Dictionary<string, string>();
                    if (inheritedVersion != null)
                    {
                        Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText("\nProcessing " + plat + " inherited version files...");
                            richTextBox1.SelectionStart = richTextBox1.Text.Length;
                            richTextBox1.ScrollToCaret();
                        }));

                        GameVersionManifest cman = null;
                        if (File.Exists(path + "/" + plat + "/versions/" + inheritedVersion + ".json"))
                            cman = JsonConvert.DeserializeObject<GameVersionManifest>(File.ReadAllText(path + "/" + plat + "/versions/" + inheritedVersion + ".json"));
                        while (cman != null)
                        {
                            foreach (string key in cman.changedFiles.Keys)
                            {
                                if (!knownHashes.ContainsKey(key) && cman.changedFiles[key] != "DELETE")
                                    knownHashes[key] = cman.changedFiles[key];
                            }

                            if (cman.previousVersion != null && File.Exists(path + "/" + plat + "/versions/" + cman.previousVersion + ".json"))
                                cman = JsonConvert.DeserializeObject<GameVersionManifest>(File.ReadAllText(path + "/" + plat + "/versions/" + cman.previousVersion + ".json"));
                            else
                                cman = null;
                        }
                    }

                    // Load current version hashes (pre-update)
                    Invoke(new Action(() =>
                    {
                        richTextBox1.AppendText("\nLoading existing " + plat + " version files...");
                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                        richTextBox1.ScrollToCaret();
                    }));
                    GameVersionManifest currentManifest = null;
                    if (File.Exists(path + "/" + plat + "/versions/" + currentVersion + ".json"))
                        currentManifest = JsonConvert.DeserializeObject<GameVersionManifest>(File.ReadAllText(path + "/" + plat + "/versions/" + currentVersion + ".json"));
                    if (currentManifest != null)
                    {
                        foreach (string key in currentManifest.changedFiles.Keys)
                        {
                            if (currentManifest.changedFiles[key] != "DELETE")
                                knownHashes[key] = currentManifest.changedFiles[key];
                        }
                    }
                    else
                    {
                        currentManifest = new GameVersionManifest()
                        {
                            changedFiles = new Dictionary<string, string>()
                        };
                    }

                    Dictionary<string, string> changedFiles = new Dictionary<string, string>();
                    List<string> knownFiles = new List<string>();

                    // Load current hashes
                    Invoke(new Action(() =>
                    {
                        richTextBox1.AppendText("\nScanning for changed " + plat + " version files...");
                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                        richTextBox1.ScrollToCaret();
                    }));
                    scanDir(input);

                    // Find deleted files
                    foreach (string key in knownHashes.Keys)
                    {
                        if (!knownFiles.Contains(key))
                        {
                            changedFiles[key] = "DELETE";
                            Invoke(new Action(() =>
                            {
                                richTextBox1.AppendText("\n[DELETED] " + key);
                                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                                richTextBox1.ScrollToCaret();
                            }));
                        }
                    }

                    // Log
                    Invoke(new Action(() =>
                    {
                        richTextBox1.AppendText("\nProcessed " + changedFiles.Count + " changes.\nUpdating manifest file...");
                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                        richTextBox1.ScrollToCaret();
                    }));

                    // Build new version lists
                    Dictionary<string, string> newChangeFiles = new Dictionary<string, string>(currentManifest.changedFiles);
                    foreach (string key in changedFiles.Keys)
                    {
                        string value = changedFiles[key];
                        newChangeFiles[key] = value;
                    }

                    // Create manifest
                    GameVersionManifest man = new GameVersionManifest();
                    man.changedFiles = newChangeFiles;
                    man.version = currentVersion;
                    man.previousVersion = inheritedVersion;

                    // Save
                    Directory.CreateDirectory(path + "/" + plat + "/versions");
                    Directory.CreateDirectory(path + "/downloads");
                    File.WriteAllText(path + "/" + plat + "/versions/" + textBox1.Text + ".json", JsonConvert.SerializeObject(man));

                    // Log
                    Invoke(new Action(() =>
                    {
                        richTextBox1.AppendText("\nCopying " + changedFiles.Where(t => t.Value != "DELETE").Count() + " assets...");
                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                        richTextBox1.ScrollToCaret();
                    }));

                    // Copy assets
                    foreach (string file in changedFiles.Keys)
                    {
                        if (changedFiles[file] == "DELETE")
                            continue;
                        File.Copy(input + "/" + file, path + "/downloads/" + changedFiles[file], true);
                    }

                    void scanDir(string folder, string pref = "")
                    {
                        // Find files
                        foreach (FileInfo file in new DirectoryInfo(folder).GetFiles())
                        {
                            // Create hash
                            SHA256 hash = SHA256.Create();
                            FileStream strm = file.OpenRead();
                            string sha256 = string.Concat(hash.ComputeHash(strm).Select(t => t.ToString("x2")));
                            strm.Close();

                            // Save hash to known files
                            knownFiles.Add(pref + file.Name);
                            if (!knownHashes.ContainsKey(pref + file.Name) || knownHashes[pref + file.Name] != sha256)
                            {
                                changedFiles[pref + file.Name] = sha256;
                                if (!knownHashes.ContainsKey(pref + file.Name))
                                    Invoke(new Action(() =>
                                    {
                                        richTextBox1.AppendText("\n[CREATED] " + pref + file.Name);
                                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                                        richTextBox1.ScrollToCaret();
                                    }));
                                else
                                    Invoke(new Action(() =>
                                    {
                                        richTextBox1.AppendText("\n[UPDATED] " + pref + file.Name);
                                        richTextBox1.SelectionStart = richTextBox1.Text.Length;
                                        richTextBox1.ScrollToCaret();
                                    }));
                            }
                            knownHashes[pref + file.Name] = sha256;
                            Thread.Sleep(10); // offset
                        }

                        // Find directories
                        foreach (DirectoryInfo dirInfo in new DirectoryInfo(folder).GetDirectories())
                            scanDir(dirInfo.FullName, pref + dirInfo.Name + "/");
                    }
                }
            }).Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Text = "";
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select Windows game files...";
            if (dialog.ShowDialog() == DialogResult.OK)
                textBox2.Text = dialog.SelectedPath;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox3.Text = "";
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select Linux game files...";
            if (dialog.ShowDialog() == DialogResult.OK)
                textBox3.Text = dialog.SelectedPath;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox4.Text = "";
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select OSX game files...";
            if (dialog.ShowDialog() == DialogResult.OK)
                textBox4.Text = dialog.SelectedPath;
        }

        private void VersionEditor_Load(object sender, EventArgs e)
        {
            textBox1.Text = currentVersion;
        }
    }
}
