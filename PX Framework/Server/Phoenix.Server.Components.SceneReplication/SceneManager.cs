using Phoenix.Common.Services;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.AsyncTasks;
using static Phoenix.Common.SceneReplication.Packets.InitialSceneReplicationStartPacket;
using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Common.Logging;

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
        internal Dictionary<Scene, Dictionary<string, SceneObjectID>> _sceneObjectMaps = new Dictionary<Scene, Dictionary<string, SceneObjectID>>();

        // Replication memory
        internal Dictionary<Scene, List<string>> _destroyedObjects = new Dictionary<Scene, List<string>>();
        internal Dictionary<Scene, Dictionary<SceneObject, string>> _spawnedPrefabs = new Dictionary<Scene, Dictionary<SceneObject, string>>();
        internal Dictionary<Scene, List<SceneObject>> _reparentedObjects = new Dictionary<Scene, List<SceneObject>>();
        internal Dictionary<Scene, List<SceneObject>> _sceneSwitchedObjects = new Dictionary<Scene, List<SceneObject>>();
        internal Dictionary<Scene, List<SceneObject>> _editedSceneObjets = new Dictionary<Scene, List<SceneObject>>();

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
            lock (_rooms)
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
                lock (_rooms)
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
                        lock (_sceneObjectMaps)
                            _sceneObjectMaps.Remove(scene.Scene);
                        lock (_destroyedObjects)
                            _destroyedObjects.Remove(scene.Scene);
                        lock (_reparentedObjects)
                            _reparentedObjects.Remove(scene.Scene);
                        lock (_sceneSwitchedObjects)
                            _sceneSwitchedObjects.Remove(scene.Scene);
                        lock (_destroyedObjects)
                            _editedSceneObjets.Remove(scene.Scene);
                        lock (_spawnedPrefabs)
                            _spawnedPrefabs.Remove(scene.Scene);
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
                    lock (_rooms)
                        _rooms[room] = roomD;
                }

                // Grab scene from memory
                SceneInfo? scene;
                lock (roomD)
                {
                    scene = GetSceneFrom(roomD, scenePath);
                    if (scene == null)
                    {
                        // Check if the scene has been loaded
                        Scene? realScene = GetRealSceneFromMemory(assetPath);
                        lock (_sceneMemory)
                        {
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
                        }

                        // Create reflective scene objects
                        List<SceneObject> objects = new List<SceneObject>();
                        foreach (SceneObject obj in realScene.Objects)
                        {
                            objects.Add(SceneObject.Reflecting(obj, null));
                        }

                        // Create scene
                        scene = new SceneInfo();
                        scene.Scene = Scene.FromObjects(scenePath, Path.GetFileNameWithoutExtension(assetPath), objects.ToArray());
                        lock (_destroyedObjects)
                            _destroyedObjects[scene.Scene] = new List<string>();
                        lock (_reparentedObjects)
                            _reparentedObjects[scene.Scene] = new List<SceneObject>();
                        lock (_sceneSwitchedObjects)
                            _sceneSwitchedObjects[scene.Scene] = new List<SceneObject>();
                        lock (_destroyedObjects)
                            _editedSceneObjets[scene.Scene] = new List<SceneObject>();
                        lock (_spawnedPrefabs)
                            _spawnedPrefabs[scene.Scene] = new Dictionary<SceneObject, string>();
                        scene.PrefabSpawnHandler = (path, sceneInst, prefab, parent) =>
                        {
                            // Add to prefab memory
                            lock (_spawnedPrefabs)
                                if (!_spawnedPrefabs[scene.Scene].ContainsKey(prefab))
                                    _spawnedPrefabs[scene.Scene][prefab] = path;

                            // Replicate
                            foreach (Connection conn in _server.ServerConnection.GetClients())
                            {
                                try
                                {
                                    SceneReplicator? repl = conn.GetObject<SceneReplicator>();
                                    if (repl != null && repl.IsSubscribedToScene(scenePath) && repl.IsSubscribedToRoom(room))
                                    {
                                        lock (repl._replicationPackets)
                                        {
                                            repl._replicationPackets.Add(new SpawnPrefabPacket()
                                            {
                                                Room = room,
                                                ScenePath = scenePath,

                                                ObjectID = prefab.ID,
                                                PrefabPath = path,
                                                ParentObjectID = parent == null ? null : parent.ID
                                            });
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    // Overflow or something
                                    Logger.GetLogger("scene-manager").Error("Failed to queue sync for " + conn, e);
                                }
                            }
                        };
                        scene.DestroyHandler = obj =>
                        {
                            // Add to destroyed object memory if needed
                            if (objects.Contains(obj))
                            {
                                lock (_destroyedObjects)
                                    if (!_destroyedObjects[scene.Scene].Contains(obj.ID))
                                        _destroyedObjects[scene.Scene].Add(obj.ID);
                            }
                            lock (_spawnedPrefabs)
                                if (_spawnedPrefabs[scene.Scene].ContainsKey(obj))
                                    _spawnedPrefabs[scene.Scene].Remove(obj);

                            // Replicate
                            foreach (Connection conn in _server.ServerConnection.GetClients())
                            {
                                try
                                {
                                    SceneReplicator? repl = conn.GetObject<SceneReplicator>();
                                    if (repl != null && repl.IsSubscribedToScene(scenePath) && repl.IsSubscribedToRoom(room))
                                    {
                                        lock (repl._replicationPackets)
                                        {
                                            repl._replicationPackets.Add(new DestroyObjectPacket()
                                            {
                                                Room = room,
                                                ScenePath = scenePath,

                                                ObjectID = obj.ID
                                            });
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    // Overflow or something
                                    Logger.GetLogger("scene-manager").Error("Failed to queue sync for " + conn, e);
                                }
                            }
                        };
                        scene.ChangeSceneHandler = (obj, oldScene, newScene) =>
                        {
                            // Add to scene change memory if needed
                            if (objects.Contains(obj))
                            {
                                lock (_sceneSwitchedObjects)
                                    if (!_sceneSwitchedObjects[scene.Scene].Contains(obj))
                                        _sceneSwitchedObjects[scene.Scene].Add(obj);
                            }

                            // Replicate
                            foreach (Connection conn in _server.ServerConnection.GetClients())
                            {
                                try
                                {
                                    SceneReplicator? repl = conn.GetObject<SceneReplicator>();
                                    if (repl != null && repl.IsSubscribedToScene(scenePath) && repl.IsSubscribedToRoom(room))
                                    {
                                        lock (repl._replicationPackets)
                                        {
                                            repl._replicationPackets.Add(new ObjectChangeScenePacket()
                                            {
                                                Room = room,
                                                ScenePath = scenePath,

                                                ObjectID = obj.ID,
                                                NewScenePath = newScene == null ? null : newScene.Path
                                            });
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    // Overflow or something
                                    Logger.GetLogger("scene-manager").Error("Failed to queue sync for " + conn, e);
                                }
                            }
                        };
                        scene.ReParentHandler = (obj, oldParent, newParent) =>
                        {
                            // Add to reparenting memory if needed
                            if (objects.Contains(obj))
                            {
                                lock (_reparentedObjects)
                                    if (!_reparentedObjects[scene.Scene].Contains(obj))
                                        _reparentedObjects[scene.Scene].Add(obj);
                            }

                            // Replicate
                            foreach (Connection conn in _server.ServerConnection.GetClients())
                            {
                                try
                                {
                                    SceneReplicator? repl = conn.GetObject<SceneReplicator>();
                                    if (repl != null && repl.IsSubscribedToScene(scenePath) && repl.IsSubscribedToRoom(room))
                                    {
                                        lock (repl._replicationPackets)
                                        {
                                            repl._replicationPackets.Add(new ReparentObjectPacket()
                                            {
                                                Room = room,
                                                ScenePath = scenePath,

                                                ObjectID = obj.ID,
                                                NewParentID = newParent == null ? null : newParent.ID
                                            });
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    // Overflow or something
                                    Logger.GetLogger("scene-manager").Error("Failed to queue sync for " + conn, e);
                                }
                            }
                        };
                        scene.ReplicationHandler = (obj, prop, key, value) =>
                        {
                            // Add to replication memory if needed
                            if (objects.Contains(obj))
                            {
                                lock (_editedSceneObjets)
                                    if (!_editedSceneObjets[scene.Scene].Contains(obj))
                                        _editedSceneObjets[scene.Scene].Add(obj);
                            }

                            // Replicate
                            foreach (Connection conn in _server.ServerConnection.GetClients())
                            {
                                try
                                {
                                    SceneReplicator? repl = conn.GetObject<SceneReplicator>();
                                    if (repl != null && repl.IsSubscribedToScene(scenePath) && repl.IsSubscribedToRoom(room))
                                    {
                                        lock (repl._replicationPackets)
                                        {
                                            switch (prop)
                                            {
                                                case ReplicatingProperty.NAME:
                                                    repl._replicationPackets.Add(new ReplicateObjectPacket()
                                                    {
                                                        Room = room,
                                                        ScenePath = scenePath,
                                                        ObjectID = obj.ID,

                                                        HasNameChanges = true,
                                                        Name = value.ToString()
                                                    });
                                                    break;
                                                case ReplicatingProperty.IS_ACTIVE:
                                                    repl._replicationPackets.Add(new ReplicateObjectPacket()
                                                    {
                                                        Room = room,
                                                        ScenePath = scenePath,
                                                        ObjectID = obj.ID,

                                                        HasActiveStatusChanges = true,
                                                        Active = (bool)value
                                                    });
                                                    break;
                                                case ReplicatingProperty.TRANSFORM:
                                                    repl._replicationPackets.Add(new ReplicateObjectPacket()
                                                    {
                                                        Room = room,
                                                        ScenePath = scenePath,
                                                        ObjectID = obj.ID,

                                                        HasTransformChanges = true,
                                                        Transform = ((Phoenix.Server.SceneReplication.Coordinates.Transform)value).ToPacketTransform()
                                                    });
                                                    break;
                                                case ReplicatingProperty.REPLICATION_DATA:
                                                    repl._replicationPackets.Add(new ReplicateObjectPacket()
                                                    {
                                                        Room = room,
                                                        ScenePath = scenePath,
                                                        ObjectID = obj.ID,

                                                        HasDataChanges = true,
                                                        Data = new Dictionary<string, object?>() { [key] = value }
                                                    });
                                                    break;
                                                case ReplicatingProperty.REPLICATION_DATA_REMOVEKEY:
                                                    repl._replicationPackets.Add(new ReplicateObjectPacket()
                                                    {
                                                        Room = room,
                                                        ScenePath = scenePath,
                                                        ObjectID = obj.ID,

                                                        HasDataChanges = true,
                                                        RemovedData = new List<string>() { value.ToString() }
                                                    });
                                                    break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    // Overflow or something
                                    Logger.GetLogger("scene-manager").Error("Failed to queue sync for " + conn, e);
                                }
                            }
                        };

                        scene.Scene.OnSpawnPrefab += scene.PrefabSpawnHandler;
                        scene.Scene.OnReparent += scene.ReParentHandler;
                        scene.Scene.OnReplicate += scene.ReplicationHandler;
                        scene.Scene.OnChangeScene += scene.ChangeSceneHandler;
                        scene.Scene.OnDestroy += scene.DestroyHandler;
                        roomD[scenePath] = scene;

                        lock (_sceneObjectMaps)
                        {
                            if (!_sceneObjectMaps.ContainsKey(scene.Scene))
                            {
                                // Build object map
                                Dictionary<string, int> ids = new Dictionary<string, int>();
                                Dictionary<string, SceneObjectID> objMap = new Dictionary<string, SceneObjectID>();
                                void scan(SceneObject[] objs)
                                {
                                    foreach (SceneObject obj in objs)
                                    {
                                        // Load map
                                        int ind = ids.GetValueOrDefault(obj.Path, 0);
                                        ids[obj.Path] = ind + 1;
                                        objMap[obj.ID] = new SceneObjectID(obj.Path, ind);

                                        // Scan children
                                        scan(obj.Children);
                                    }
                                }
                                scan(realScene.Objects);
                                _sceneObjectMaps[scene.Scene] = objMap;
                            }
                        }

                        // Run scene update
                        AsyncTaskManager.RunAsync(() =>
                        {
                            while (true)
                            {
                                    // Tick scene
                                    scene.Scene.Tick();

                                    // Replicate
                                    foreach (Connection conn in _server.ServerConnection.GetClients())
                                {
                                    SceneReplicator? repl = conn.GetObject<SceneReplicator>();
                                    if (repl != null)
                                        repl.Sync();
                                }

                                    // Wait
                                    Thread.Sleep(5);
                            }
                        });
                    }

                    loadingScene = false;
                    return scene.Scene;
                }
            }
            finally
            {
                loadingScene = false;
            }
        }
    }
}