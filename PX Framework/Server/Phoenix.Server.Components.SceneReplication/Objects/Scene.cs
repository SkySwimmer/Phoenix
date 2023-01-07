using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Phoenix.Server.SceneReplication
{
    /// <summary>
    /// Replicated scene object
    /// </summary>
    public class Scene
    {
        private string? room;
        private SceneManager? manager;

        private string name;
        private string path;
        private List<SceneObject> objects = new List<SceneObject>();

        internal Dictionary<string, string?> _spawnedPrefabs = new Dictionary<string, string?>();
        internal string[] SpawnedPrefabs
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _spawnedPrefabs.Keys.ToArray();
                    }
                    catch { }
                }
            }
        }

        internal Dictionary<string, SceneObject?> _reparentedObjects = new Dictionary<string, SceneObject?>();
        internal string[] ReparentedObjects
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _reparentedObjects.Keys.ToArray();
                    }
                    catch { }
                }
            }
        }

        internal Dictionary<string, string> _newObjectScenes = new Dictionary<string, string>();
        internal string[] NewObjectScenes
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _newObjectScenes.Keys.ToArray();
                    }
                    catch { }
                }
            }
        }

        internal List<string> _destroyedObjects = new List<string>();
        internal string[] DestroyedObjects
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _destroyedObjects.ToArray();
                    }
                    catch { }
                }
            }
        }

        private Scene(string path, string name, SceneObject[] objects, SceneManager? manager, string? room)
        {
            this.manager = manager;
            this.room = room;
            foreach (SceneObject obj in objects)
            {
                SetScene(obj);
            }
            this.objects.AddRange(objects);
            this.name = name;
            this.path = path;
        }

        private void SetScene(SceneObject obj)
        {
            obj.SetScene(this);
            foreach (SceneObject ch in obj.Children)
                SetScene(ch);
        }

        public delegate void DestroyHandler(SceneObject sender);
        public delegate void ChangeSceneHandler(SceneObject sender, Scene? oldScene, Scene? newScene);
        public delegate void ReParentHandler(SceneObject sender, SceneObject? oldParent, SceneObject? newParent);
        public delegate void ReplicationHandler(SceneObject sender, ReplicatingProperty property, object? value, string? key = null);

        /// <summary>
        /// Reads a PRISM prefab file and adds it to the scene (uses the asset manager)
        /// </summary>
        /// <param name="filePath">Prefab path (without .prpm)</param>
        /// <returns>SceneObject instance</returns>
        public SceneObject SpawnPrefab(string filePath)
        {
            string assetPath = "SceneReplication/" + filePath + ".prpm";
            string prefabData;
            try
            {
                prefabData = AssetManager.GetAssetString(assetPath);
            }
            catch
            {
                throw new ArgumentException("Prefab not found: " + filePath);
            }
            SceneObject obj = SceneObject.FromJson(prefabData, manager, room);
            objects.Add(obj);
            if (manager != null && room != null)
                manager.ReplicateAddPrefab(obj, filePath, room, this, null);
            return obj;
        }

        /// <summary>
        /// Called when a object requires to be replicated (called when properties change)
        /// </summary>
        public event ReplicationHandler? OnReplicate;

        /// <summary>
        /// Called when a object is destroyed
        /// </summary>
        public event DestroyHandler? OnDestroy;

        /// <summary>
        /// Called when a object is reparented
        /// </summary>
        public event ReParentHandler? OnReparent;

        /// <summary>
        /// Called when a object is moved to a different scene
        /// </summary>
        public event ChangeSceneHandler? OnChangeScene;

        /// <summary>
        /// Reads a scene from a PRISM scene map
        /// </summary>
        /// <param name="path">Scene path</param>
        /// <param name="name">Scene name</param>
        /// <param name="json">PRISM-encoded scene map</param>
        /// <returns>Scene object</returns>
        public static Scene FromJson(string path, string name, string json)
        {
            List<SceneObject> sceneObjects = new List<SceneObject>();
            JObject[]? objs = JsonConvert.DeserializeObject<JObject[]>(json);
            if (objs != null)
            {
                foreach (JObject obj in objs)
                {
                    sceneObjects.Add(SceneObject.FromJson(JsonConvert.SerializeObject(obj)));
                }
            }
            return new Scene(path, name, sceneObjects.ToArray(), null, null);
        }

        /// <summary>
        /// Creates a scene container
        /// </summary>
        /// <param name="path">Scene path</param>
        /// <param name="name">Scene name</param>
        /// <param name="objects">Scene objects</param>
        /// <param name="manager">Scene manager</param>
        /// <param name="room">Room ID</param>
        /// <returns>Scene object</returns>
        internal static Scene FromObjects(string path, string name, SceneObject[] objects, SceneManager manager, string? room)
        {
            return new Scene(path, name, objects, manager, room);
        }

        internal void AddToScene(SceneObject obj)
        {
            objects.Add(obj);
        }

        internal void RemoveFromScene(SceneObject obj)
        {
            objects.Remove(obj);
        }

        internal void HandleOnDestroy(SceneObject sender)
        {
            OnDestroy?.Invoke(sender);
        }

        internal void HandleOnChangeScene(SceneObject sender, Scene? oldScene, Scene? newScene)
        {
            OnChangeScene?.Invoke(sender, oldScene, newScene);
        }

        internal void HandleReparent(SceneObject sender, SceneObject? oldParent, SceneObject? newParent)
        {
            OnReparent?.Invoke(sender, oldParent, newParent);
        }

        internal void HandleReplicate(SceneObject sender, ReplicatingProperty property, object? value, string? key)
        {
            OnReplicate?.Invoke(sender, property, value, key);
        }

        public override string ToString()
        {
            return "Scene " + Name;
        }

        /// <summary>
        /// Retrieves the scene name
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Retrieves the scene path
        /// </summary>
        public string Path
        {
            get
            {
                return path;
            }
        }

        /// <summary>
        /// Retrieves the objects of this scene
        /// </summary>
        public SceneObject[] Objects
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return objects.ToArray();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Retrieves objects by name
        /// </summary>
        /// <param name="name">Object name</param>
        /// <returns>SceneObject instance</returns>
        public SceneObject GetObject(string name)
        {
            SceneObject? res = GetObjectOrNull(name);
            if (res == null)
                throw new ArgumentException("Object not found");
            return res;
        }

        /// <summary>
        /// Retrieves objects by name
        /// </summary>
        /// <param name="name">Object name</param>
        /// <returns>SceneObject instance or null</returns>
        public SceneObject? GetObjectOrNull(string name)
        {
            if (name.Contains("/"))
            {
                string pth = name.Remove(name.IndexOf("/"));
                string ch = name.Substring(name.IndexOf("/") + 1);
                foreach (SceneObject obj in Objects)
                {
                    if (obj.Name == pth)
                    {
                        SceneObject t = obj.GetChild(ch);
                        if (t != null)
                            return t;
                    }
                }
                return null;
            }
            foreach (SceneObject obj in Objects)
            {
                if (obj.Name == name)
                    return obj;
            }
            return null;
        }
    }
}
