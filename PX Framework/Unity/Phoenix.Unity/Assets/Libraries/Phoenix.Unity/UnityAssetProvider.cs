using System;
using System.IO;
using Phoenix.Server;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO.Compression;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Phoenix.Unity.Bindings
{

#if UNITY_EDITOR

    // Unity editor bindings
    public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            // Delete remapped assets
            try
            {
                Directory.Delete("Assets/Resources/PhoenixRemapped", true);
            }
            catch
            {
                Directory.Delete("Assets/Resources/PhoenixRemapped");
            }
            File.Delete("Assets/Resources/PhoenixRemapped.meta");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (Directory.Exists("Assets/Resources/PhoenixRemapped"))
            {
                try
                {
                    Directory.Delete("Assets/Resources/PhoenixRemapped", true);
                }
                catch
                {
                    Directory.Delete("Assets/Resources/PhoenixRemapped");
                }
            }

            // Compile a list of all resources
            Debug.Log("Building Phoenix asset manifest...");
            List<string> assets = new List<string>();
            Directory.CreateDirectory("Assets/Resources/PhoenixAssets");  
            Scan(new DirectoryInfo("Assets/Resources/PhoenixAssets"), "", assets);
            if (assets.Contains("phoenixassetmanifest.json"))
                assets.Remove("phoenixassetmanifest.json");
            File.WriteAllText("Assets/Resources/phoenixassetmanifest.json", JsonConvert.SerializeObject(assets));

            // Remap incompatible assets to textassets
            ScanIncompatible(new DirectoryInfo("Assets/Resources/PhoenixAssets"), "");
        }

        private void Scan(DirectoryInfo dir, string prefix, List<string> assets)
        {
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Name.EndsWith(".meta"))
                    continue;
                assets.Add(prefix + file.Name);
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                Scan(subDir, prefix + subDir.Name + "/", assets);
            }
        }

        private void ScanIncompatible(DirectoryInfo dir, string prefix)
        {
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Name.EndsWith(".meta"))
                    continue;

                // Try loading the resource
                try
                {
                    TextAsset asset = Resources.Load<TextAsset>(prefix + Path.GetFileNameWithoutExtension(file.Name));
                    if (asset == null)
                        throw new Exception("Cannot load this asset");

                    // Check if it needs compiling
                    try
                    {
                        FileStream strm = file.OpenRead();
                        Stream assetStrm = AssetCompiler.Compile(strm, file.Name);
                        if (assetStrm != strm)
                        {
                            // Create output and log
                            Directory.CreateDirectory("Assets/Resources/PhoenixRemapped/" + prefix);
                            Debug.Log("Remapping and compressing " + prefix + file.Name + " to a text asset...");

                            // Compress
                            if (File.Exists("Assets/Resources/PhoenixRemapped/" + prefix + file.Name + ".txt"))
                                File.Delete("Assets/Resources/PhoenixRemapped/" + prefix + file.Name + ".txt");
                            FileStream output = File.OpenWrite("Assets/Resources/PhoenixRemapped/" + prefix + file.Name + ".txt");
                            GZipStream gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal);
                            assetStrm.CopyTo(gzip);
                            gzip.Close();
                            output.Close();
                        }
                        assetStrm.Close();
                        try
                        {
                            strm.Close();
                        }
                        catch { }
                    }
                    catch
                    {
                        Debug.LogError("Failed to compile asset: " + file.Name);
                    }
                }
                catch
                {
                    // Create output and log
                    Directory.CreateDirectory("Assets/Resources/PhoenixRemapped/" + prefix);
                    Debug.Log("Remapping and compressing " + prefix + file.Name + " to a text asset...");

                    // Compress
                    FileStream input = File.OpenRead(file.FullName);
                    if (File.Exists("Assets/Resources/PhoenixRemapped/" + prefix + file.Name + ".txt"))
                        File.Delete("Assets/Resources/PhoenixRemapped/" + prefix + file.Name + ".txt");
                    FileStream output = File.OpenWrite("Assets/Resources/PhoenixRemapped/" + prefix + file.Name + ".txt");
                    GZipStream gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal);
                    input.CopyTo(gzip);
                    gzip.Close();
                    output.Close();
                    input.Close();
                }
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                if (prefix == "" && subDir.Name == "PhoenixRemapped")
                    continue;
                ScanIncompatible(subDir, prefix + subDir.Name + "/");
            }
        }
    }

#endif

    public class UnityAssetProvider : IAssetProvider
    {
        public string[] GetAssetsIn(string folder)
        {
#if UNITY_EDITOR
            // Find on disk
            if (Directory.Exists("Assets/Resources/PhoenixAssets/" + folder))
            {
                List<string> assets = new List<string>();
                foreach (FileInfo file in new DirectoryInfo("Assets/Resources/PhoenixAssets/" + folder).GetFiles())
                {
                    if (!file.Name.EndsWith(".meta") && (!file.Name.Equals("phoenixassetmanifest.json") || folder != ""))
                        assets.Add(file.Name);
                }
                return assets.ToArray();
            }
            return new string[0];
#endif
#if !UNITY_EDITOR
            // Find in manifest
            List<string> assets = new List<string>();
            TextAsset asset = Resources.Load<TextAsset>("phoenixassetmanifest");
            List<string> manifest = JsonConvert.DeserializeObject<List<string>>(asset.text);
            string folderStr = folder;
            if (folderStr != "")
                folderStr += "/";
            foreach (string assetPath in manifest)
            {
                if (assetPath.StartsWith(folderStr) && !assetPath.Substring(folderStr.Length + 1).Contains("/"))
                    assets.Add(assetPath);
            }
            return assets.ToArray();
#endif
        }

        public Stream GetAssetStream(string asset)
        {
#if UNITY_EDITOR
            // Load from disk
            if (File.Exists("Assets/Resources/PhoenixAssets/" + asset))
                return AssetCompiler.Compile(File.OpenRead("Assets/Resources/PhoenixAssets/" + asset), asset);
            return null;
#endif
#if !UNITY_EDITOR
            // Find asset
            string dir = Path.GetDirectoryName(asset);
            if (dir != "")
                dir += "/";
            string assetPath = "PhoenixAssets/" + dir + Path.GetFileNameWithoutExtension(asset);
            try
            {
                TextAsset data = Resources.Load<TextAsset>(assetPath);
                if (data == null)
                    throw new Exception();
                return new MemoryStream(data.bytes);
            }
            catch
            {
                // Find Phoenix remapped asset
                assetPath = "PhoenixRemapped/" + asset;
                try
                {
                    TextAsset data = Resources.Load<TextAsset>(assetPath);
                    if (data == null)
                        return null;

                    // Decompress
                    MemoryStream assetData = new MemoryStream(data.bytes);
                    GZipStream strm = new GZipStream(assetData, CompressionMode.Decompress);
                    MemoryStream output = new MemoryStream();
                    strm.CopyTo(output);
                    strm.Close();
                    assetData = new MemoryStream(output.ToArray());
                    output.Dispose();
                    return assetData;
                }
                catch
                {
                    return null;
                }
            }
#endif
        }
    }
}
