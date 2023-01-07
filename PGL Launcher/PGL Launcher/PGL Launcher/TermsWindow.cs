using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PGL_Launcher
{
    public partial class TermsWindow : Form
    {
        public bool Accepted = false;

        public TermsWindow(string title, string terms)
        {
            InitializeComponent();
            richTextBox1.Text = terms;
            label1.Text = title + " - Terms of Service";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            button1.Enabled = checkBox1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Accepted = true;
            Close();
        }
    }
}
