using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phoenix.SceneReplication.Editor
{
    public class PrefabDumper
    {
        [MenuItem("Phoenix/Create PRISM Prefab Map(s)/From Asset Bundle (Recommended, Fast)")]
        public static void DumpPrefabs()
        {
            if (!AssetDatabase.GetAllAssetBundleNames().Any(t => t == "phoenix.replication"))
            {
                Debug.LogError("Missing asset bundle: phoenix.replication");
                return;
            }
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            string[] assets = AssetDatabase.GetAssetPathsFromAssetBundle("phoenix.replication");
            foreach (string asset in assets)
            {
                try
                {
                    if (!asset.EndsWith(".prefab"))
                        continue;
                    if (!asset.StartsWith("Assets/Resources/"))
                    {
                        Debug.LogError("Invalid asset: " + asset + ": requires to be placed in resources");
                        continue;
                    }
                    PrefabUtility.LoadPrefabContentsIntoPreviewScene(asset, previewScene);
                    GameObject obj = previewScene.GetRootGameObjects()[0];

                    // Dump prefab
                    List<object> data = new List<object>();
                    SceneDumper.DumpObject(obj, data);
                    GameObject.DestroyImmediate(obj, false);
                    string encoded = JsonConvert.SerializeObject(data[0], Formatting.Indented);
                    string p = asset.Substring("Assets/Resources/".Length);
                    if (!(bool)((Dictionary<string, object>)data[0])["replicating"])
                    {
                        Debug.Log("Skipped prefab: " + p + ": root object does not replicate");
                        continue;
                    }
                    p = p.Remove(p.LastIndexOf(".prefab"));
                    string path = "Assets/Resources/PhoenixAssets/SceneReplication/" + p + ".prpm";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, encoded);
                    Debug.Log("Dumped to " + path);
                }
                catch
                {
                    Debug.LogWarning("Not a prefab: " + asset);
                }
            }
            EditorSceneManager.ClosePreviewScene(previewScene);
        }

        [MenuItem("Phoenix/Create PRISM Prefab Map(s)/From Resource Folder")]
        public static void DumpPrefabsFromAssets()
        {
            if (!Directory.Exists("Assets/Resources"))
            {
                Debug.LogError("No resources folder in Assets");
                return;
            }
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            ReadPrefabs(new DirectoryInfo("Assets/Resources"), "", previewScene);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }

        private static void ReadPrefabs(DirectoryInfo dir, string prefix, Scene previewScene)
        {
            foreach (FileInfo assetFile in dir.GetFiles("*.prefab"))
            {
                string p = prefix + assetFile.Name;

                try
                {
                    PrefabUtility.LoadPrefabContentsIntoPreviewScene("Assets/Resources/" + p, previewScene);
                    GameObject obj = previewScene.GetRootGameObjects()[0];

                    // Dump prefab
                    List<object> data = new List<object>();
                    SceneDumper.DumpObject(obj, data);
                    GameObject.DestroyImmediate(obj, false);
                    if (!(bool)((Dictionary<string, object>)data[0])["replicating"])
                    {
                        Debug.Log("Skipped prefab: " + p + ": root object does not replicate");
                        continue;
                    }
                    string encoded = JsonConvert.SerializeObject(data[0], Formatting.Indented);
                    p = p.Remove(p.LastIndexOf(".prefab"));
                    string path = "Assets/Resources/PhoenixAssets/SceneReplication/" + p + ".prpm";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, encoded);
                    Debug.Log("Dumped to " + path);
                }
                catch
                {
                    Debug.LogWarning("Not a prefab: Assets/Resources/" + p);
                }
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                ReadPrefabs(subDir, prefix + subDir.Name + "/", previewScene);
            }
        }
    }
}
