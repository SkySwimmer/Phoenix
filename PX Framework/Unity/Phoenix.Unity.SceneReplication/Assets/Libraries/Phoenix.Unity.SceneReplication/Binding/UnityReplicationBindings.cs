using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Common.Tasks;
using System.IO;

namespace Phoenix.Unity.SceneReplication
{
    /// <summary>
    /// Phoenix replication bindings for Unity
    /// </summary>
    public class UnityReplicationBindings : SceneReplicationBindings
    {
        private SceneReplicationComponent component;

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
        public delegate void BeginInitialSyncEventHandler(string room, string scenePath);

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
            component.ServiceManager.GetService<TaskManager>().Oneshot(action);
        }

        public override IReplicatingSceneObject GetObjectInScene(string room, string scenePath, string objectPath)
        {
            try
            {
                Scene scene = SceneManager.GetSceneByPath("Assets/" + scenePath + ".unity");
                GameObject obj = GameObjectUtils.GetObjectInScene(scene, objectPath);
                if (obj != null)
                {
                    try
                    {
                        ReplicatedObject rep = obj.GetComponent<ReplicatedObject>();
                        if (rep != null)
                            return rep;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return null;
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
        public override void OnBeginInitialSync(string room, string scenePath)
        {
            OnInitialSyncStart?.Invoke(room, scenePath);
        }

        public override void OnFinishInitialSync(string room, string scenePath)
        {
            OnInitialSyncFinish?.Invoke(room, scenePath);
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
                    if (packet.ParentObjectPath != null)
                    {
                        GameObject obj = GameObjectUtils.GetObjectInScene(scene, packet.ParentObjectPath);
                        if (obj != null)
                            prefab.transform.parent = obj.transform;
                    }
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

    }
}
