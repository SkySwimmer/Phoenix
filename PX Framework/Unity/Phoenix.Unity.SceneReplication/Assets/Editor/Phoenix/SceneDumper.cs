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
    public class SceneDumper
    {
        [MenuItem("Phoenix/Create PRISM SceneMap(s)/Current Scene")]
        public static void DumpCurrent()
        {
            DumpScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Phoenix/Create PRISM SceneMap(s)/All Scenes/From Build Settings (Recommended, Fast)")]
        public static void DumpAll()
        {
            string old = SceneManager.GetActiveScene().path;
            List<string> scenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene sc = SceneManager.GetSceneAt(i);
                if (!scenes.Contains(sc.path))
                {
                    scenes.Add(sc.path);
                    DumpScene(sc);
                }
            }
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i));
                Scene sc = SceneManager.GetActiveScene();
                if (!scenes.Contains(sc.path))
                {
                    scenes.Add(sc.path);
                    DumpScene(sc);
                }
            }
            EditorSceneManager.OpenScene(old);
        }

        [MenuItem("Phoenix/Create PRISM SceneMap(s)/All Scenes/From Asset Library")]
        public static void DumpAllFromAssets()
        {
            string old = SceneManager.GetActiveScene().path;
            foreach (FileInfo sceneFile in new DirectoryInfo("Assets").GetFiles("*.unity", SearchOption.AllDirectories))
            {
                EditorSceneManager.OpenScene(sceneFile.FullName);
                Scene sc = SceneManager.GetActiveScene();
                DumpScene(sc);
            }
            EditorSceneManager.OpenScene(old);
        }

        private static void DumpScene(Scene scene)
        {
            Directory.CreateDirectory("Assets/Resources/PhoenixAssets/SceneReplication");

            // Dump scene
            List<object> data = new List<object>();
            foreach (GameObject obj in scene.GetRootGameObjects()) {
                DumpObject(obj, data);
            }
            string encoded = JsonConvert.SerializeObject(data, Formatting.Indented);
            string p = scene.path.Substring("Assets/".Length);
            p = p.Remove(p.LastIndexOf(".unity"));
            string path = "Assets/Resources/PhoenixAssets/SceneReplication/" + p + ".prsm";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, encoded);
            Debug.Log("Dumped to " + path);
        }

        internal static void DumpObject(GameObject obj, List<object> data)
        {
            // Get transform
            Transform tr = obj.GetComponent<Transform>();
            ReplicatedObject repInfo = obj.GetComponent<ReplicatedObject>();

            // Create object entry
            Dictionary<string, object> objectData = new Dictionary<string, object>();
            objectData["name"] = obj.name;
            objectData["replicating"] = repInfo != null;
            objectData["active"] = obj.activeSelf;

            // Add transform
            Dictionary<string, object> transform = new Dictionary<string, object>();
            transform["position"] = new Dictionary<string, object>() { 
                ["x"] = tr.localPosition.x,
                ["y"] = tr.localPosition.y,
                ["z"] = tr.localPosition.z
            };
            transform["rotation"] = new Dictionary<string, object>()
            {
                ["x"] = tr.localRotation.x,
                ["y"] = tr.localRotation.y,
                ["z"] = tr.localRotation.z,
                ["w"] = tr.localRotation.w
            };
            transform["angles"] = new Dictionary<string, object>()
            {
                ["x"] = tr.localEulerAngles.x,
                ["y"] = tr.localEulerAngles.y,
                ["z"] = tr.localEulerAngles.z
            };
            transform["scale"] = new Dictionary<string, object>()
            {
                ["x"] = tr.localScale.x,
                ["y"] = tr.localScale.y,
                ["z"] = tr.localScale.z
            };
            objectData["transform"] = transform;

            // Add replication information
            Dictionary<string, object> replication = new Dictionary<string, object>();
            if (repInfo != null)
                repInfo.SerializeInto(replication);
            objectData["replication"] = replication;

            // Add children
            List<object> children = new List<object>();
            objectData["children"] = children;
            Transform[] trs = obj.GetComponentsInChildren<Transform>(true);
            foreach (Transform trCh in trs)
            {
                if (trCh.parent == tr.gameObject.transform)
                    DumpObject(trCh.gameObject, children);
            }

            // Add entry
            data.Add(objectData);
        }
    }
}
