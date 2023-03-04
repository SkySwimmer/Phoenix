using Phoenix.Common.Services;
using Phoenix.Common.Networking.Connections;

namespace Phoenix.Server.SceneReplication
{
    /// <summary>
    /// Scene Manager
    /// </summary>
    public class SceneManager : IService
    {
        private GameServer _server;

        public SceneManager(GameServer server)
        {
            _server = server;
        }

        private class SceneInfo
        {
            public Scene Scene;
            public ReplicationHandler ReplicationHandler;
            public ReParentHandler ReParentHandler;
            public DestroyHandler DestroyHandler;
            public ChangeSceneHandler ChangeSceneHandler;
            public SpawnPrefabHandler PrefabSpawnHandler;
        }

        private Dictionary<string, Scene> _sceneMemory = new Dictionary<string, Scene>();
        private Dictionary<string, Dictionary<string, SceneInfo>> _rooms = new Dictionary<string, Dictionary<string, SceneInfo>>();

        private Scene? GetRealSceneFromMemory(string scene)
        {
            if (!_sceneMemory.ContainsKey(scene))
                return null;
            while (true)
            {
                try
                {
                    return _sceneMemory[scene];
                }
                catch { }
            }
        }

        private Dictionary<string, SceneInfo>? GetRoom(string room)
        {
            while (true)
            {
                try
                {
                    if (_rooms.ContainsKey(room))
                        return _rooms[room];
                    return null;
                }
                catch { }
            }
        }

        private SceneInfo? GetSceneFrom(Dictionary<string, SceneInfo> room, string scene)
        {
            while (true)
            {
                try
                {
                    if (room == null)
                        return null;
                    if (room.ContainsKey(scene))
                        return room[scene];
                    return null;
                }
                catch { }
            }
        }

        /// <summary>
        /// Checks if a room exists
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>True if present, false otherwise</returns>
        public bool RoomExists(string id)
        {
            return _rooms.ContainsKey(id);
        }

        /// <summary>
        /// Deletes rooms
        /// </summary>
        /// <param name="id">Room ID</param>
        public void DeleteRoom(string id)
        {
            if (RoomExists(id))
            {
                Dictionary<string, SceneInfo>? room = GetRoom(id);
                _rooms.Remove(id);

                // Unbind replication events
                if (room != null)
                {
                    foreach (SceneInfo scene in room.Values)
                    {
                        scene.Scene.OnReparent -= scene.ReParentHandler;
                        scene.Scene.OnReplicate -= scene.ReplicationHandler;
                        scene.Scene.OnChangeScene -= scene.ChangeSceneHandler;
                        scene.Scene.OnDestroy -= scene.DestroyHandler;
                        scene.Scene.OnSpawnPrefab -= scene.PrefabSpawnHandler;
                        foreach (SceneObject obj in scene.Scene.Objects)
                        {
                            obj.DisposeInternal();
                        }
                    }
                }

                // Desubscribe all clients
                foreach (Connection client in _server.ServerConnection.GetClients())
                {
                    SceneReplicator? repl = client.GetObject<SceneReplicator>();
                    if (repl != null)
                    {
                        if (repl.IsSubscribedToRoom(id))
                            repl.DesubscribeFromRoom(id);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all active room IDs
        /// </summary>
        public string[] RoomIDs
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _rooms.Keys.ToArray();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Retrieves all loaded scenes
        /// </summary>
        /// <param name="room">Room ID</param>
        /// <returns>Array of Scene instances</returns>
        public Scene[] GetLoadedScenes(string room = "DEFAULT")
        {
            Dictionary<string, SceneInfo>? roomD = GetRoom(room);
            if (roomD == null)
                return new Scene[0];
            while (true)
            {
                try
                {
                    return roomD.Values.Select(t => t.Scene).ToArray();
                }
                catch { }
            }
        }

        /// <summary>
        /// Reads a PRISM prefab file (uses the asset manager, <b>this does not replicate, use Scene.SpawnPrefab or GameObject.SpawnPrefab to add prefabs to the world</b>)
        /// </summary>
        /// <param name="filePath">Prefab path (without .prpm)</param>
        /// <returns>SceneObject instance</returns>
        public SceneObject ReadPrefab(string filePath)
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
            return SceneObject.FromJson(prefabData);
        }

        private bool loadingScene = false;

        /// <summary>
        /// Retrieves scenes from memory or reads a PRSIM file if not in memory yet (uses the asset manager)
        /// </summary>
        /// <param name="scenePath">Scene asset path (without .prsm, reads from subdirectory SceneReplication, eg. <u>Scenes/SampleScene</u>)</param>
        /// <param name="room">Room ID</param>
        /// <returns>Scene instance</returns>
        public Scene GetScene(string scenePath, string room = "DEFAULT")
        {
            while (loadingScene)
                Thread.Sleep(100);
            loadingScene = true;
            try
            {
                string assetPath = "SceneReplication/" + scenePath + ".prsm";
                Dictionary<string, SceneInfo>? roomD = GetRoom(room);
                if (roomD == null)
                {
                    roomD = new Dictionary<string, SceneInfo>();
                    _rooms[room] = roomD;
                }

                // Grab scene from memory
                SceneInfo? scene = GetSceneFrom(roomD, scenePath);
                if (scene == null)
                {
                    // Check if the scene has been loaded
                    Scene? realScene = GetRealSceneFromMemory(assetPath);
                    if (realScene == null)
                    {
                        // Load scene
                        string sceneData;
                        try
                        {
                            sceneData = AssetManager.GetAssetString(assetPath);
                        }
                        catch
                        {
                            loadingScene = false;
                            throw new ArgumentException("Scene not found: " + scenePath);
                        }
                        try
                        {
                            realScene = Scene.FromJson(scenePath, Path.GetFileNameWithoutExtension(assetPath), sceneData);
                        }
                        catch
                        {
                            loadingScene = false;
                            throw new ArgumentException("Invalid scene file: " + assetPath);
                        }
                        _sceneMemory[assetPath] = realScene;
                    }

                    // Create reflective scene objects
                    List<SceneObject> objects = new List<SceneObject>();
                    foreach (SceneObject obj in realScene.Objects)
                    {
                        // TODO: FIXME: HAVE TO CHANGE THIS
                        objects.Add(obj); //objects.Add(SceneObject.Reflecting(obj, null)); 
                    }

                    // Create scene
                    scene = new SceneInfo();
                    scene.Scene = Scene.FromObjects(scenePath, Path.GetFileNameWithoutExtension(assetPath), objects.ToArray());
                    scene.PrefabSpawnHandler = (path, sceneInst, prefab, parent) =>
                    {
                        // TODO
                        path = path;
                    };
                    scene.DestroyHandler = obj =>
                    {
                        // TODO
                        obj = obj;
                    };
                    scene.ChangeSceneHandler += (obj, oldScene, newScene) =>
                    {
                        // TODO
                        obj = obj;
                    };
                    scene.ReParentHandler += (obj, oldParent, newParent) =>
                    {
                        // TODO
                        obj = obj;
                    };
                    scene.ReplicationHandler += (obj, prop, value, key) =>
                    {
                        // TODO
                        obj = obj;
                    };
                    scene.Scene.OnSpawnPrefab += scene.PrefabSpawnHandler;
                    scene.Scene.OnReparent += scene.ReParentHandler;
                    scene.Scene.OnReplicate += scene.ReplicationHandler;
                    scene.Scene.OnChangeScene += scene.ChangeSceneHandler;
                    scene.Scene.OnDestroy += scene.DestroyHandler;
                    roomD[scenePath] = scene;
                }

                loadingScene = false;
                return scene.Scene;
            }
            finally
            {
                loadingScene = false;
            }
        }
    }
}
