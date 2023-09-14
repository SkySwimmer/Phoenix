using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PGL_Launcher
{
    public class GameVersionManifest
    {
        public string version;
        public string previousVersion = null;
        public Dictionary<string, string> changedFiles = new Dictionary<string, string>();

        public string ToString()
        {
            return version;
        }
    }
}
