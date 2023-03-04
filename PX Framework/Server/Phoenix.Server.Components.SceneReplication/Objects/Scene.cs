using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Phoenix.Server.SceneReplication
{
    /// <summary>
    /// Event handler for prefab spawning
    /// </summary>
    /// <param name="filePath">Prefab path</param>
    /// <param name="scene">Scene the prefab was spawned in</param>
    /// <param name="prefab">Prefab object</param>
    /// <param name="parent">Parent object</param>
    public delegate void SpawnPrefabHandler(string filePath, Scene scene, SceneObject prefab, SceneObject? parent);

    /// <summary>
    /// Replicated scene object
    /// </summary>
    public class Scene
    {
        private string _name;
        private string _path;

        private Dictionary<string, SceneObject> _objects = new Dictionary<string, SceneObject>();
        
        private Scene(string name, string path)
        {
            _name = name;
            _path = path;
        }

        private Scene(string path, string name, SceneObject[] objects)
        {
            foreach (SceneObject obj in objects)
            {
                AddToScene(obj);
            }
            _name = name;
            _path = path;
        }

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
            return new Scene(path, name, sceneObjects.ToArray());
        }

        /// <summary>
        /// Creates a scene container
        /// </summary>
        /// <param name="path">Scene path</param>
        /// <param name="name">Scene name</param>
        /// <param name="objects">Scene objects</param>
        /// <returns>Scene object</returns>
        internal static Scene FromObjects(string path, string name, SceneObject[] objects)
        {
            return new Scene(path, name, objects);
        }

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
            SceneObject obj = SceneObject.FromJson(prefabData);
            obj.Scene = this;
            HandleSpawnPrefab(filePath, obj, null);
            return obj;
        }

        /// <summary>
        /// Retrieves the scene name
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Retrieves the scene path
        /// </summary>
        public string Path
        {
            get
            {
                return _path;
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
                        return _objects.Values.ToArray();
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
        /// <returns>Array of SceneObject instances</returns>
        public SceneObject[] GetObjects(string name)
        {
            if (name.Contains("/"))
            {
                string pth = name.Remove(name.IndexOf("/"));
                string ch = name.Substring(name.IndexOf("/") + 1);
                foreach (SceneObject obj in Objects)
                {
                    if (obj.Name == pth)
                    {
                        SceneObject[] objs = obj.GetChildren(ch);
                        if (objs.Length != 0)
                            return objs;
                    }
                }
                return new SceneObject[0];
            }
            return Objects.Where(t => t.Name == name).ToArray();
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

        /// <summary>
        /// Ticks the scene
        /// </summary>
        public void Tick()
        {
            foreach (SceneObject obj in Objects)
            {
                obj.Update();
            }
        }

        /// <summary>
        /// Called when this object or a child of it requires to be replicated (called when properties change)
        /// </summary>
        public event ReplicationHandler? OnReplicate;

        /// <summary>
        /// Called when this object or a child of it is destroyed
        /// </summary>
        public event DestroyHandler? OnDestroy;

        /// <summary>
        /// Called when this object or a child of it is reparented
        /// </summary>
        public event ReParentHandler? OnReparent;

        /// <summary>
        /// Called when this object or a child of it is moved to a different scene
        /// </summary>
        public event ChangeSceneHandler? OnChangeScene;

        /// <summary>
        /// Called when a prefab is spawned in the scene
        /// </summary>
        public event SpawnPrefabHandler? OnSpawnPrefab;

        public override string ToString()
        {
            return "Scene " + Name;
        }

        internal void AddToScene(SceneObject sender)
        {
            // Set scene and add
            lock (_objects)
                _objects[sender.ID] = sender;
            sender.InternalSetScene(this);
        }

        internal void RemoveFromScene(SceneObject sender)
        {
            // Unset scene and remove
            lock (_objects)
                _objects.Remove(sender.ID);
            sender.InternalUnsetScene();
        }

        internal void HandleOnDestroy(SceneObject sender)
        {
            OnDestroy?.Invoke(sender);
        }

        internal void HandleReplicate(SceneObject sender, ReplicatingProperty property, string? key, object? value)
        {
            OnReplicate?.Invoke(sender, property, key, value);
        }

        internal void HandleOnChangeScene(SceneObject sender, Scene oldScene, Scene? newScene)
        {
            OnChangeScene?.Invoke(sender, oldScene, newScene);
        }

        internal void HandleReparent(SceneObject sender, SceneObject? oldParent, SceneObject? newParent)
        {
            OnReparent?.Invoke(sender, oldParent, newParent);
        }

        internal void HandleSpawnPrefab(string filePath, SceneObject prefab, SceneObject? parent)
        {
            OnSpawnPrefab?.Invoke(filePath, this, prefab, parent);
        }
    }
}
