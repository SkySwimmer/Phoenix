using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PGL_Launcher
{
    public partial class LauncherEditor : Form
    {
        public LauncherEditor()
        {
            InitializeComponent();
        }

        private string path = null;
        private Dictionary<string, GameVersionManifest> winVersions = new Dictionary<string, GameVersionManifest>();
        private Dictionary<string, GameVersionManifest> linuxVersions = new Dictionary<string, GameVersionManifest>();
        private Dictionary<string, GameVersionManifest> osxVersions = new Dictionary<string, GameVersionManifest>();

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == tabPage2 && path == null)
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "Select or open existing server directory...\nOpening a existing folder will load the existing versions into the editor.";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.SelectedPath;
                    LoadVersions();
                    return;
                }
                tabControl1.SelectedIndex = 0; // go back when cancelled
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Make user pick destination
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select output directory";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Create launcher
                StringWriter wr = new StringWriter();
                wr.WriteLine("GameID=" + textBox1.Text);
                wr.WriteLine("DigitalSeal=" + textBox2.Text);
                wr.WriteLine("Banner=" + textBox3.Text);
                wr.WriteLine("ProductEncryption=" + textBox4.Text);
                if (textBox5.Text != "")
                    wr.WriteLine("GameVersion=" + textBox5.Text);
                if (Program.CreateLauncher(wr.ToString(), dialog.SelectedPath))
                    MessageBox.Show("Successfully created the launcher!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select or open existing server directory...\nOpening a existing folder will load the existing versions into the editor.";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                path = dialog.SelectedPath;
            }
            LoadVersions();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Create a new version?\n\nNOTE: The selected version will be used as 'previous version' for update file resolution.\n\nProceed with creation?", "Create version", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                VersionCreator creator = new VersionCreator();
                creator.path = path;
                creator.inheritedVersion = (string)listBox1.SelectedItem;
                creator.winVersions = winVersions;
                creator.linuxVersions = linuxVersions;
                creator.osxVersions = osxVersions;
                creator.ShowDialog();
                if (creator.result != null)
                    listBox1.Items.Add(creator.result);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this version?\n\nNOTE: download files will not be deleted, only the version file gets deleted.\n\nProceed with deletion?", "Delete version", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string version = listBox1.SelectedItem.ToString();

                // Remove from list
                listBox1.Items.Remove(listBox1.SelectedItem);
                button3.Enabled = false;
                button4.Enabled = false;

                // Delete files
                if (File.Exists(path + "/windows/versions/" + version + ".json"))
                    File.Delete(path + "/windows/versions/" + version + ".json");
                if (File.Exists(path + "/linux/versions/" + version + ".json"))
                    File.Delete(path + "/linux/versions/" + version + ".json");
                if (File.Exists(path + "/osx/versions/" + version + ".json"))
                    File.Delete(path + "/osx/versions/" + version + ".json");
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button3.Enabled = listBox1.SelectedItem != null;
            button4.Enabled = listBox1.SelectedItem != null;
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void LoadVersions()
        {
            button3.Enabled = false;
            button4.Enabled = false;
            listBox1.Items.Clear();
            winVersions.Clear();
            linuxVersions.Clear();
            osxVersions.Clear();

            // Load windows version files
            if (Directory.Exists(path + "/windows/versions"))
                Scan(path + "/windows/versions", winVersions);
            // Load linux version files
            if (Directory.Exists(path + "/linux/versions"))
                Scan(path + "/linux/versions", linuxVersions);
            // Load osx version files
            if (Directory.Exists(path + "/osx/versions"))
                Scan(path + "/osx/versions", osxVersions);

            void Scan(string folder, Dictionary<string, GameVersionManifest> manifests)
            {
                // Find version files
                foreach (FileInfo file in new DirectoryInfo(folder).GetFiles("*.json"))
                {
                    // Create version object
                    GameVersionManifest man = JsonConvert.DeserializeObject<GameVersionManifest>(File.ReadAllText(file.FullName));
                    man.version = file.Name.Remove(file.Name.LastIndexOf(".json"));
                    manifests[man.version] = man;
                    if (!listBox1.Items.Contains(man.version))
                        listBox1.Items.Add(man.version);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Create a minor update?\n\nNOTE: The existing version will be modified, which will cause clients to update the game.\n\nProceed with creation?", "Create version", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                VersionEditor editor = new VersionEditor();
                editor.path = path;
                editor.currentVersion = (string)listBox1.SelectedItem;
                editor.winVersions = winVersions;
                editor.linuxVersions = linuxVersions;
                editor.osxVersions = osxVersions;
                editor.ShowDialog();
            }
        }
    }
}
