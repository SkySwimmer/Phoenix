using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Common.Tasks;
using System.IO;
using System.Collections.Generic;
using Phoenix.Client.SceneReplicatorLib.Messages;
using Phoenix.Common;
using System.Linq;
using Component = UnityEngine.Component;
using Phoenix.Unity.PGL.Internal;

namespace Phoenix.Unity.SceneReplication
{
    /// <summary>
    /// Phoenix replication bindings for Unity
    /// </summary>
    public class UnityReplicationBindings : SceneReplicationBindings
    {
        private SceneReplicationComponent component;
        private Dictionary<string, GameObject> _objects = new Dictionary<string, GameObject>();

        private class PhoenixReplicationCleanup : MonoBehaviour
        {
            public string ID;
            public string Room;
            public UnityReplicationBindings Bindings;

            public void OnDestroy()
            {
                if (ID != null)
                    lock(Bindings._objects)
                        Bindings._objects.Remove(ID);
            }
        }

        /// <summary>
        /// Creates the scene replication binding instance (typically automatically called by the replication component)
        /// </summary>
        /// <param name="comp">SceneReplicationComponent instance</param>
        public UnityReplicationBindings(SceneReplicationComponent comp)
        {
            component = comp;

            // Assign the default scene loader
            SceneLoader = (scenePath, additive) =>
            {
                try
                {
                    UnityEngine.Events.UnityAction<Scene, LoadSceneMode> loadHandler = null;
                    loadHandler = (sc, m) => {
                        if (sc.path.ToLower() == "assets/" + scenePath.ToLower() + ".unity")
                        {
                            if (component.IsSceneLoading(scenePath))
                                component.FinishLoadingScene(scenePath);
                            SceneManager.sceneLoaded -= loadHandler;
                        }
                    };
                    SceneManager.sceneLoaded += loadHandler;
                    SceneManager.LoadSceneAsync(scenePath, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                    return true;
                }
                catch
                {
                    return false;
                }
            };

            // Assign the default scene unloader
            SceneUnloader = (scenePath) =>
            {
                try
                {
                    SceneManager.UnloadSceneAsync(scenePath);
                    return true;
                }
                catch
                {
                    return false;
                }
            };

            // Bind events to the scene manager
            SceneManager.sceneLoaded += (sc, m) =>
            {
                string scenePath = sc.path;
                scenePath = Path.GetDirectoryName(scenePath) + "/" + Path.GetFileNameWithoutExtension(scenePath);
                if (scenePath.ToLower().StartsWith("assets/"))
                    scenePath = scenePath.Substring("assets/".Length);
                if (!comp.IsSceneLoading(scenePath) && !comp.IsSceneLoaded(scenePath))
                    comp.FinishLoadingScene(scenePath);
            };
            SceneManager.sceneUnloaded += (sc) =>
            {
                string scenePath = sc.path;
                scenePath = Path.GetDirectoryName(scenePath) + "/" + Path.GetFileNameWithoutExtension(scenePath);
                if (scenePath.ToLower().StartsWith("assets/"))
                    scenePath = scenePath.Substring("assets/".Length);
                if (comp.IsSceneLoaded(scenePath))
                    comp.UnloadScene(scenePath);
            };

            // Add loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene sc = SceneManager.GetSceneAt(i);

                string scenePath = sc.path;
                scenePath = Path.GetDirectoryName(scenePath) + "/" + Path.GetFileNameWithoutExtension(scenePath);
                if (scenePath.ToLower().StartsWith("assets/"))
                    scenePath = scenePath.Substring("assets/".Length);
                if (!comp.IsSceneLoading(scenePath) && !comp.IsSceneLoaded(scenePath))
                    comp.FinishLoadingScene(scenePath);
            }
        }

        /// <summary>
        /// Called to load a scene
        /// </summary>
        /// <param name="scenePath">Scene path</param>
        /// <param name="additive">True for additive scene loading, false otherwise</param>
        /// <returns>True if the scene successfully started loading, false if invalid</returns>
        public delegate bool SceneLoadHandler(string scenePath, bool additive);

        /// <summary>
        /// Called to unload a scene
        /// </summary>
        /// <param name="scenePath">Scene path</param>
        /// <returns>True if the scene successfully started unloading, false if invalid</returns>
        public delegate bool SceneUnloadHandler(string scenePath);

        /// <summary>
        /// Event handler for when scene load requests are received
        /// </summary>
        /// <param name="scenePath">Scene path</param>
        /// <param name="additive">True if additive, false otherwise</param>
        public delegate void SceneLoadRequestEventHandler(string scenePath, bool additive);

        /// <summary>
        /// Event handler for when scene unload requests are received
        /// </summary>
        /// <param name="scenePath">Scene path</param>
        public delegate void SceneUnloadRequestEventHandler(string scenePath);

        /// <summary>
        /// Event handler for when initial sync begins
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        public delegate void BeginInitialSyncEventHandler(string room, string scenePath, Dictionary<string, InitialSceneReplicationStartPacket.SceneObjectID> objectMap);

        /// <summary>
        /// Event handler for when initial sync finishes
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        public delegate void FinishInitialSyncEventHandler(string room, string scenePath);

        /// <summary>
        /// The scene load handler called to load scenes
        /// </summary>
        public SceneLoadHandler SceneLoader;

        /// <summary>
        /// The scene unload handler called to unload scenes
        /// </summary>
        public SceneUnloadHandler SceneUnloader;

        /// <summary>
        /// Called when a scene load request is received, run before the actual loading process (assign SceneLoader to change the default loading behaviour)
        /// </summary>
        public event SceneLoadRequestEventHandler OnSceneLoadRequest;

        /// <summary>
        /// Called when a scene unload request is received, run before the actual unloading process (assign SceneUnloader to change the default unloading behaviour)
        /// </summary>
        public event SceneUnloadRequestEventHandler OnSceneUnloadRequest;

        /// <summary>
        /// Called when initial sync starts
        /// </summary>
        public event BeginInitialSyncEventHandler OnInitialSyncStart;

        /// <summary>
        /// Called when initial sync finishes
        /// </summary>
        public event FinishInitialSyncEventHandler OnInitialSyncFinish;

        public override string GetName()
        {
            return "Phoenix.Unity.SceneReplication";
        }

        public override void RunOnNextFrameUpdate(Action action)
        {
            PGL_TickUtil.Schedule(action);
        }

        public override void LoadScene(string scenePath, bool additive)
        {
            // Begin loading
            component.BeginLoadingScene(scenePath);

            // Schedule action
            RunOnNextFrameUpdate(() =>
            {
                // Call event
                OnSceneLoadRequest?.Invoke(scenePath, additive);

                // Perform scene loading
                if (!SceneLoader(scenePath, additive))
                    component.CancelLoadingScene(scenePath);
            });
        }

        public override void UnloadScene(string scenePath)
        {
            // Schedule action
            RunOnNextFrameUpdate(() =>
            {
                // Call event
                OnSceneUnloadRequest?.Invoke(scenePath);

                // Perform scene loading
                SceneUnloader(scenePath);
                component.UnloadScene(scenePath);
            });
        }
        public override void OnBeginInitialSync(string room, string scenePath, Dictionary<string, InitialSceneReplicationStartPacket.SceneObjectID> objectMap)
        {
            OnInitialSyncStart?.Invoke(room, scenePath, objectMap);

            // Find all objects in the scene from the object map
            try
            {
                Scene scene = SceneManager.GetSceneByPath("Assets/" + scenePath + ".unity");

                // Assign ids for each object
                AssignIds(objectMap, scene.GetRootGameObjects(), "", new Dictionary<string, int>(), room);
            }
            catch
            {
                // Unity goof
            }
        }

        private void AssignIds(Dictionary<string, InitialSceneReplicationStartPacket.SceneObjectID> objectMap, 
            GameObject[] gameObjects, string prefixPath, Dictionary<string, int> indexMemory, string room)
        {
            Phoenix.Common.Logging.Logger logger = Phoenix.Common.Logging.Logger.GetLogger("scene-replication");
            foreach (GameObject obj in gameObjects)
            {
                // Scan children
                AssignIds(objectMap, obj.GetChildren(), prefixPath + obj.name + "/", indexMemory, room);

                // Build path
                string path = prefixPath + obj.name;

                // Find index
                int index = indexMemory.GetValueOrDefault(path, 0);
                indexMemory[path] = index + 1;

                // Find ID
                bool found = false;
                foreach (string id in objectMap.Keys)
                {
                    if (objectMap[id].Path == path && objectMap[id].Index == index)
                    {
                        // Found the ID
                        logger.Debug("Assigned ID " + id + " to " + path + " (index " + index + ")");
                        found = true;

                        // Save the object to memory
                        lock(_objects)
                            _objects[id] = obj;

                        // Make sure its removed on destroy
                        PhoenixReplicationCleanup c = obj.GetComponents<PhoenixReplicationCleanup>().Where(t => t.Room == room).FirstOrDefault();
                        if (c == null)
                            c = obj.AddComponent<PhoenixReplicationCleanup>();
                        c.Bindings = this;
                        c.ID = id;

                        break;
                    }
                }

                if (!found)
                {
                    // Error
                    logger.Error("Could not assign network ID to " + path + " (index " + index + "), replication loading failure!");
                    if (Game.DebugMode)
                        logger.Error("Please verify if the client is not out of sync, if you are certain the server version matches, please re-dump the scene via the Phoenix tools in the Editor.");
                }
            }
        }

        public override void OnFinishInitialSync(string room, string scenePath)
        {
            OnInitialSyncFinish?.Invoke(room, scenePath);
        }

        public override void CreateObject(CreateObjectPacket packet)
        {
            try
            {
                Scene scene = SceneManager.GetSceneByPath("Assets/" + packet.ScenePath + ".unity");
                try
                {
                    GameObject obj = new GameObject();
                    obj.name = packet.ObjectName;
                    obj.AddComponent<ReplicatedObject>();                    
                    SceneManager.MoveGameObjectToScene(obj, scene);
                    GameObject parent = null;
                    if (packet.ParentObjectID != null)
                        lock (_objects)
                            if (_objects.ContainsKey(packet.ParentObjectID))
                                parent = _objects[packet.ParentObjectID];
                    if (parent != null)
                        obj.transform.parent = parent.transform;

                    // Apply fields
                    obj.SetActive(packet.Active);
                    if (obj.transform.localPosition != packet.Transform.Position.ToUnityVector3())
                        obj.transform.localPosition = packet.Transform.Position.ToUnityVector3();
                    if (obj.transform.localEulerAngles != packet.Transform.Rotation.ToUnityVector3())
                        obj.transform.localEulerAngles = packet.Transform.Rotation.ToUnityVector3();
                    if (obj.transform.localScale != packet.Transform.Scale.ToUnityVector3())
                        obj.transform.localScale = packet.Transform.Scale.ToUnityVector3();
                    obj.GetComponent<ReplicatedObject>().DeserializeFrom(packet.Data);

                    // Save the object to memory
                    lock (_objects)
                        _objects[packet.ObjectID] = obj;

                    // Make sure its removed on destroy
                    PhoenixReplicationCleanup c = obj.GetComponents<PhoenixReplicationCleanup>().Where(t => t.Room == packet.Room).FirstOrDefault();
                    if (c == null)
                        c = obj.AddComponent<PhoenixReplicationCleanup>();
                    c.Bindings = this;
                    c.ID = packet.ObjectID;
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public override void SpawnPrefab(SpawnPrefabPacket packet)
        {
            try
            {
                Scene scene = SceneManager.GetSceneByPath("Assets/" + packet.ScenePath + ".unity");
                try
                {
                    GameObject prefab = GameObject.Instantiate(Resources.Load<GameObject>(packet.PrefabPath));
                    prefab.name = Path.GetFileNameWithoutExtension(packet.PrefabPath);
                    SceneManager.MoveGameObjectToScene(prefab, scene);
                    GameObject parent = null;
                    if (packet.ParentObjectID != null)
                        lock (_objects)
                            if (_objects.ContainsKey(packet.ParentObjectID))
                                parent = _objects[packet.ParentObjectID];
                    if (parent != null)
                        prefab.transform.parent = parent.transform;

                    // Apply fields
                    prefab.SetActive(packet.Active);
                    if (prefab.transform.localPosition != packet.Transform.Position.ToUnityVector3())
                        prefab.transform.localPosition = packet.Transform.Position.ToUnityVector3();
                    if (prefab.transform.localEulerAngles != packet.Transform.Rotation.ToUnityVector3())
                        prefab.transform.localEulerAngles = packet.Transform.Rotation.ToUnityVector3();
                    if (prefab.transform.localScale != packet.Transform.Scale.ToUnityVector3())
                        prefab.transform.localScale = packet.Transform.Scale.ToUnityVector3();
                    prefab.GetComponent<ReplicatedObject>().DeserializeFrom(packet.Data);

                    // Save the object to memory
                    lock (_objects)
                        _objects[packet.ObjectID] = prefab;

                    // Make sure its removed on destroy
                    PhoenixReplicationCleanup c = prefab.GetComponents<PhoenixReplicationCleanup>().Where(t => t.Room == packet.Room).FirstOrDefault();
                    if (c == null)
                        c = prefab.AddComponent<PhoenixReplicationCleanup>();
                    c.Bindings = this;
                    c.ID = packet.ObjectID;
                }
                catch
                {
                }
            }
            catch
            {
            }
        }


        public override IReplicatingSceneObject GetObjectByIDInScene(string room, string scenePath, string objectID)
        {
            try
            {
                Scene scene = SceneManager.GetSceneByPath("Assets/" + scenePath + ".unity");
                GameObject obj = null;
                lock(_objects)
                {
                    if (_objects.ContainsKey(objectID))
                        obj = _objects[objectID];
                }
                if (obj.scene.path == scene.path)
                {
                    ReplicatedObject rep = obj.GetComponent<ReplicatedObject>();
                    if (rep != null)
                        return rep;
                }
            }
            catch
            {
            }
            return null;
        }

        public override IComponentMessageReceiver[] GetNetworkedComponents(string room, string scenePath, string objectID)
        {
            try
            {
                Scene scene = SceneManager.GetSceneByPath("Assets/" + scenePath + ".unity");
                GameObject obj = null;
                lock (_objects)
                {
                    if (_objects.ContainsKey(objectID))
                        obj = _objects[objectID];
                }
                if (obj.scene.path == scene.path)
                {
                    ReplicatedObject rep = obj.GetComponent<ReplicatedObject>();
                    if (rep != null)
                        return obj.GetComponents<Component>().Where(t => t is IComponentMessageReceiver).Select(t => (IComponentMessageReceiver)t).ToArray();
                }
            }
            catch
            {
            }
            return new IComponentMessageReceiver[0];
        }

        private string GetPath(GameObject obj)
        {
            if (obj.transform.parent != null)
                return GetPath(obj.transform.parent.gameObject) + "/" + obj.name;
            return obj.name;
        }

        public override string GetObjectPathByID(string room, string scenePath, string objectID)
        {
            try
            {
                Scene scene = SceneManager.GetSceneByPath("Assets/" + scenePath + ".unity");
                GameObject obj = null;
                lock (_objects)
                {
                    if (_objects.ContainsKey(objectID))
                        obj = _objects[objectID];
                }
                if (obj.scene.path == scene.path)
                {
                    ReplicatedObject rep = obj.GetComponent<ReplicatedObject>();
                    if (rep != null)
                        return GetPath(obj);
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
