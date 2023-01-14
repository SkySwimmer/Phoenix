using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PGL_Launcher
{
    public static class IOUtils
    {
        /// <summary>
        /// Copies folders
        /// </summary>
        /// <param name="source">Source path</param>
        /// <param name="dest">Destination path</param>
        public static void CopyDirectory(string source, string dest, string ignore = "")
        {
            if (ignore != "" && Path.GetFullPath(source.Replace('/', Path.DirectorySeparatorChar)) == Path.GetFullPath(ignore))
                return;

            Directory.CreateDirectory(dest);

            // Copy files
            foreach (FileInfo file in new DirectoryInfo(source).GetFiles())
                file.CopyTo(dest + "/" + file.Name);

            // Copy directories
            foreach (DirectoryInfo dir in new DirectoryInfo(source).GetDirectories())
                CopyDirectory(dir.FullName, dest + "/" + dir.Name, ignore);
        }

        /// <summary>
        /// Deletes folders
        /// </summary>
        /// <param name="dir">Directory path</param>
        public static void DeleteDirectory(string dir)
        {
            if (!Directory.Exists(dir))
                return;

            // Delete files
            foreach (FileInfo file in new DirectoryInfo(dir).GetFiles())
                file.Delete();

            // Delete directories
            foreach (DirectoryInfo sub in new DirectoryInfo(dir).GetDirectories())
                DeleteDirectory(sub.FullName);
            Directory.Delete(dir);
        }
    }
}
