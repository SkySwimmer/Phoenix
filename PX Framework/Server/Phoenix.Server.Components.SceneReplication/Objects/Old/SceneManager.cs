using Phoenix.Common;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication;
using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Common.Services;
using Phoenix.Server.Components.SceneReplication.Old.Objects.ReplicationQueue;
using Phoenix.Server.SceneReplication.Coordinates;
using Transform = Phoenix.Server.SceneReplication.Coordinates.Transform;

namespace Phoenix.Server.SceneReplication.Old
{
    /// <summary>
    /// Scene Manager
    /// </summary>
    public class SceneManager : IService
    {
        private GameServer _server;
        private List<ReplicationCommand> _replicationCommands = new List<ReplicationCommand>();

        /// <summary>
        /// Retrieves the replication queue
        /// </summary>
        public ReplicationCommand[] ReplicationQueue
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _replicationCommands.ToArray();
                    }
                    catch { }
                }
            }
        }

        public SceneManager(GameServer server)
        {
            _server = server;
        }

        private bool _replicating = false;

        private void SendReplicationPacket(AbstractNetworkPacket pkt)
        {
            // Retrieve packet channel
            SceneReplicationChannel channel;
            try
            {
                channel = _server.ServerConnection.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }
            channel.SendPacket(pkt);
        }

        internal void ReplicateAddPrefab(SceneObject prefabObj, string prefab, string room, Scene scene, SceneObject? parent)
        {
            // Add prefab
            SendReplicationPacket(new SpawnPrefabPacket()
            {
                Room = room,
                ScenePath = scene.Path,

                PrefabPath = prefab,
                ParentObjectPath = parent == null ? null : parent.Path
            });

            // Attach events
            Scene sc = scene;
            prefabObj.SetScene(sc);
            SceneObject.DestroyHandler onDestroy = null;
            SceneObject.ChangeSceneHandler onChangeScene = null;
            SceneObject.ReParentHandler onReparent = null;
            onDestroy = obj =>
            {
                if (obj == prefabObj)
                {
                    if (sc._spawnedPrefabs.ContainsKey(prefab))
                        sc._spawnedPrefabs.Remove(prefab);
                    obj.OnDestroy -= onDestroy;
                    obj.OnChangeScene -= onChangeScene;
                    obj.OnReparent -= onReparent;
                }
            };
            onReparent = (obj, oldParent, newParent) =>
            {
                if (obj == prefabObj)
                {
                    sc._spawnedPrefabs[prefab] = newParent == null ? null : newParent.Path;
                }
            };
            onChangeScene = (obj, oldScene, newScene) =>
            {
                if (obj == prefabObj)
                {
                    if (sc._spawnedPrefabs.ContainsKey(prefab))
                        sc._spawnedPrefabs.Remove(prefab);
                    if (newScene != null)
                    {
                        newScene._spawnedPrefabs[prefab] = null;
                        sc = newScene;
                    }
                }
            };
            prefabObj.OnReparent += onReparent;
            prefabObj.OnDestroy += onDestroy;
            prefabObj.OnChangeScene += onChangeScene;
            sc._spawnedPrefabs[prefab] = prefabObj.Parent == null ? null : prefabObj.Parent.Path;
        }

        private class SceneInfo
        {
            public Scene Scene;
            public Scene.ReplicationHandler ReplicationHandler;
            public Scene.ReParentHandler ReParentHandler;
            public Scene.DestroyHandler DestroyHandler;
            public Scene.ChangeSceneHandler ChangeSceneHandler;
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
        /// Checks if the server is currently replicating scenes to clients
        /// </summary>
        public bool IsReplicating
        {
            get
            {
                return _replicating;
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
                        objects.Add(SceneObject.Reflecting(obj, null, this, room));
                    }

                    // Create scene
                    scene = new SceneInfo();
                    scene.Scene = Scene.FromObjects(scenePath, Path.GetFileNameWithoutExtension(assetPath), objects.ToArray(), this, room);
                    scene.DestroyHandler = obj =>
                    {
                        if (ReplicationQueue.Length > 10000000)
                            Logger.GetLogger("scene-manager").Warn("Detected large amounts of scene replication commands within one server tick, nearing overload level!");
                        lock (_replicationCommands)
                            _replicationCommands.Add(new ReplicationCommand()
                            {
                                Room = room,
                                Scene = scenePath,
                                ObjectPath = obj.Path,
                                Type = ReplicationCommandType.DESTROY
                            });
                    };
                    scene.ChangeSceneHandler += (obj, oldScene, newScene) =>
                    {
                        if (ReplicationQueue.Length > 10000000)
                            Logger.GetLogger("scene-manager").Warn("Detected large amounts of scene replication commands within one server tick, nearing overload level!");
                        lock (_replicationCommands)
                            _replicationCommands.Add(new ReplicationCommand()
                            {
                                Room = room,
                                Scene = oldScene == null ? scene.Scene.Path : oldScene.Path,
                                ObjectPath = obj.Path,
                                Type = ReplicationCommandType.CHANGE_SCENE,
                                Data = newScene == null ? null : newScene.Path
                            });
                    };
                    scene.ReParentHandler += (obj, oldParent, newParent) =>
                    {
                        if (ReplicationQueue.Length > 10000000)
                            Logger.GetLogger("scene-manager").Warn("Detected large amounts of scene replication commands within one server tick, nearing overload level!");
                        lock (_replicationCommands)
                            _replicationCommands.Add(new ReplicationCommand()
                            {
                                Room = room,
                                Scene = scenePath,
                                ObjectPath = oldParent == null ? obj.Name : oldParent.Path + "/" + obj.Name,
                                Type = ReplicationCommandType.REPARENT,
                                Data = new object?[] {
                            oldParent == null ? null : oldParent.Path,
                            newParent == null ? null : newParent.Path
                        }
                            });
                    };
                    scene.ReplicationHandler += (obj, prop, value, key) =>
                    {
                        if (ReplicationQueue.Length > 10000000)
                            Logger.GetLogger("scene-manager").Warn("Detected large amounts of scene replication commands within one server tick, nearing overload level!");
                        lock (_replicationCommands)
                            _replicationCommands.Add(new ReplicationCommand()
                            {
                                Room = room,
                                Scene = scenePath,
                                ObjectPath = obj.Path,
                                Type = ReplicationCommandType.REPLICATE,
                                Data = new object?[] {
                            prop,
                            key,
                            value
                        }
                            });
                    };
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

        /// <summary>
        /// Replicates all scenes currently in queue
        /// </summary>
        public void ReplicateNow()
        {
            if (_replicating)
                return;
            _replicating = true;

            // Find changes to replicate and build final replication map
            List<ReplicationDataframe> replicationDataframes = new List<ReplicationDataframe>();
            ObjectReplicationDataframe? replicationDataframe = null;
            string? lastScene = null;
            string? lastObjectPath = null;
            string? lastRoom = null;
            ReplicationCommand[] queue = ReplicationQueue;
            foreach (ReplicationCommand command in queue)
            {
                if (command == null)
                    continue;
                if (RoomExists(command.Room))
                {
                    switch (command.Type)
                    {
                        case ReplicationCommandType.DESTROY:
                            {
                                if (replicationDataframe != null)
                                {
                                    replicationDataframes.Add(replicationDataframe);
                                    replicationDataframe = null;
                                    lastScene = null;
                                    lastObjectPath = null;
                                    lastRoom = null;
                                }
                                replicationDataframes.Add(new ObjectDestroyDataframe()
                                {
                                    ObjectPath = command.ObjectPath,
                                    Room = command.Room,
                                    ScenePath = command.Scene
                                });
                                break;
                            }
                        case ReplicationCommandType.REPLICATE:
                            {
                                if (replicationDataframe != null)
                                {
                                    if (lastScene != command.Scene && lastRoom != command.Room && lastObjectPath != command.ObjectPath)
                                    {
                                        replicationDataframes.Add(replicationDataframe);
                                        replicationDataframe = new ObjectReplicationDataframe();
                                        replicationDataframe.Room = command.Room;
                                        replicationDataframe.ScenePath = command.Scene;
                                        replicationDataframe.ObjectPath = command.ObjectPath;
                                        lastObjectPath = command.ObjectPath;
                                        lastScene = command.Scene;
                                        lastRoom = command.Room;
                                    }
                                }
                                else
                                {
                                    replicationDataframe = new ObjectReplicationDataframe();
                                    replicationDataframe.Room = command.Room;
                                    replicationDataframe.ScenePath = command.Scene;
                                    replicationDataframe.ObjectPath = command.ObjectPath;
                                    lastObjectPath = command.ObjectPath;
                                    lastScene = command.Scene;
                                    lastRoom = command.Room;
                                }

                                object[] data = (object[])command.Data;
                                ReplicatingProperty prop = (ReplicatingProperty)data[0];
                                string? key = (string)data[1];
                                object? value = (object)data[2];
                                switch (prop)
                                {
                                    case ReplicatingProperty.NAME:
                                        replicationDataframe.HasNameChanges = true;
                                        replicationDataframe.Name = value.ToString();
                                        break;
                                    case ReplicatingProperty.IS_ACTIVE:
                                        replicationDataframe.HasActiveStatusChanges = true;
                                        replicationDataframe.Active = (bool)value;
                                        break;
                                    case ReplicatingProperty.TRANSFORM_POSITION:
                                        replicationDataframe.HasTransformChanges = true;
                                        replicationDataframe.Transform = (Transform)value;
                                        break;
                                    case ReplicatingProperty.TRANSFORM_ROTATION:
                                        replicationDataframe.HasTransformChanges = true;
                                        replicationDataframe.Transform = (Transform)value;
                                        break;
                                    case ReplicatingProperty.TRANSFORM_SCALE:
                                        replicationDataframe.HasTransformChanges = true;
                                        replicationDataframe.Transform = (Transform)value;
                                        break;
                                    case ReplicatingProperty.REPLICATION_DATA:
                                        replicationDataframe.HasDataChanges = true;
                                        replicationDataframe.Data[key] = value;
                                        if (replicationDataframe.RemovedData.Contains(key))
                                            replicationDataframe.RemovedData.Remove(key);
                                        break;
                                    case ReplicatingProperty.REPLICATION_DATA_REMOVEKEY:
                                        replicationDataframe.HasDataChanges = true;
                                        replicationDataframe.RemovedData.Add(value.ToString());
                                        if (replicationDataframe.Data.ContainsKey(value.ToString()))
                                            replicationDataframe.Data.Remove(value.ToString());
                                        break;
                                }
                                break;
                            }
                        case ReplicationCommandType.REPARENT:
                            {
                                if (replicationDataframe != null)
                                {
                                    replicationDataframes.Add(replicationDataframe);
                                    replicationDataframe = null;
                                    lastScene = null;
                                    lastObjectPath = null;
                                    lastRoom = null;
                                }
                                replicationDataframes.Add(new ObjectReparentDataframe()
                                {
                                    ObjectPath = command.ObjectPath,
                                    Room = command.Room,
                                    ScenePath = command.Scene,
                                    OldParentPath = (string?)((object?[])command.Data)[0],
                                    NewParentPath = (string?)((object?[])command.Data)[1]
                                });
                                break;
                            }
                        case ReplicationCommandType.CHANGE_SCENE:
                            {
                                if (replicationDataframe != null)
                                {
                                    replicationDataframes.Add(replicationDataframe);
                                    replicationDataframe = null;
                                    lastScene = null;
                                    lastObjectPath = null;
                                    lastRoom = null;
                                }
                                replicationDataframes.Add(new ObjectSceneChangeDataframe()
                                {
                                    ObjectPath = command.ObjectPath,
                                    Room = command.Room,
                                    ScenePath = command.Scene,
                                    NewScenePath = (string?)command.Data
                                });
                                break;
                            }
                        case ReplicationCommandType.SPAWN_PREFAB:
                            {
                                if (replicationDataframe != null)
                                {
                                    replicationDataframes.Add(replicationDataframe);
                                    replicationDataframe = null;
                                    lastScene = null;
                                    lastObjectPath = null;
                                    lastRoom = null;
                                }
                                replicationDataframes.Add(new SpawnPrefabDataframe()
                                {
                                    ObjectPath = command.ObjectPath,
                                    Room = command.Room,
                                    ScenePath = command.Scene,
                                    PrefabPath = (string?)command.Data
                                });
                                break;
                            }
                    }
                }
                lock (_replicationCommands)
                    _replicationCommands.Remove(command);
            }
            if (replicationDataframe != null)
                replicationDataframes.Add(replicationDataframe);

            // Replicate to subscribed clients
            if (replicationDataframes.Count != 0)
            {
                Connection[] clients = _server.ServerConnection.GetClients();
                foreach (Connection conn in clients)
                {
                    // Retrieve packet channel
                    SceneReplicationChannel channel;
                    try
                    {
                        channel = conn.GetChannel<SceneReplicationChannel>();
                    }
                    catch
                    {
                        throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
                    }
                        try
                    {
                        if (conn != null)
                        {
                            SceneReplicator repl = conn.GetObject<SceneReplicator>();
                            if (repl != null && replicationDataframes.Any(t => repl.SubscribedRooms.Any(t2 => t.Room == t2)))
                            {
                                // Wait
                                while (repl.IsReplicating)
                                    Thread.Sleep(1);
                                repl._replicating = true;

                                // Replicate scenes
                                try
                                {
                                    // Go through rooms
                                    foreach (string room in repl.SubscribedRooms)
                                    {
                                        repl._presentlyReplicatingRoom = room;
                                        repl._presentlyReplicatingScene = null;
                                        repl._presentlyReplicatingSceneObject = null;

                                        // Go through scenes
                                        foreach (string scene in repl.SubscribedScenes)
                                        {
                                            repl._presentlyReplicatingScene = scene;
                                            repl._presentlyReplicatingSceneObject = null;

                                            // Replicate
                                            bool first = true;
                                            try
                                            {
                                                foreach (ReplicationDataframe frame in replicationDataframes)
                                                {
                                                    if (frame.ScenePath == scene && frame.Room == room)
                                                    {
                                                        if (first)
                                                        {
                                                            first = false;
                                                            channel.SendPacket(new SceneReplicationStartPacket()
                                                            {
                                                                Room = room,
                                                                ScenePath = scene
                                                            });
                                                        }
                                                        // Send dataframe
                                                        switch (frame.Type)
                                                        {
                                                            case ReplicationCommandType.DESTROY:
                                                                {
                                                                    ObjectDestroyDataframe f = (ObjectDestroyDataframe)frame;
                                                                    repl._presentlyReplicatingSceneObject = f.ObjectPath;
                                                                    channel.SendPacket(new DestroyObjectPacket()
                                                                    {
                                                                        Room = room,
                                                                        ScenePath = scene,
                                                                        ObjectID = f.ObjectPath
                                                                    });
                                                                    break;
                                                                }
                                                            case ReplicationCommandType.REPLICATE:
                                                                {
                                                                    ObjectReplicationDataframe f = (ObjectReplicationDataframe)frame;
                                                                    repl._presentlyReplicatingSceneObject = f.ObjectPath;
                                                                    channel.SendPacket(new ReplicateObjectPacket()
                                                                    {
                                                                        Room = room,
                                                                        ScenePath = scene,
                                                                        ObjectID = f.ObjectPath,

                                                                        HasActiveStatusChanges = f.HasActiveStatusChanges,
                                                                        HasDataChanges = f.HasDataChanges,
                                                                        HasNameChanges = f.HasNameChanges,
                                                                        HasTransformChanges = f.HasTransformChanges,

                                                                        Transform = f.Transform == null ? null : f.Transform.ToPacketTransform(),
                                                                        Active = f.Active,
                                                                        Data = f.Data,
                                                                        Name = f.Name,
                                                                        RemovedData = f.RemovedData
                                                                    });
                                                                    break;
                                                                }
                                                            case ReplicationCommandType.REPARENT:
                                                                {
                                                                    ObjectReparentDataframe f = (ObjectReparentDataframe)frame;
                                                                    repl._presentlyReplicatingSceneObject = f.ObjectPath;
                                                                    channel.SendPacket(new ReparentObjectPacket()
                                                                    {
                                                                        Room = room,
                                                                        ScenePath = scene,
                                                                        ObjectID = f.ObjectPath,

                                                                        NewParentPath = f.NewParentPath,
                                                                        OldParentPath = f.OldParentPath
                                                                    });
                                                                    break;
                                                                }
                                                            case ReplicationCommandType.CHANGE_SCENE:
                                                                {
                                                                    ObjectSceneChangeDataframe f = (ObjectSceneChangeDataframe)frame;
                                                                    repl._presentlyReplicatingSceneObject = f.ObjectPath;
                                                                    channel.SendPacket(new ObjectChangeScenePacket()
                                                                    {
                                                                        Room = room,
                                                                        ScenePath = scene,
                                                                        ObjectID = f.ObjectPath,
                                                                        NewScenePath = f.NewScenePath
                                                                    });
                                                                    break;
                                                                }
                                                            case ReplicationCommandType.SPAWN_PREFAB:
                                                                {
                                                                    SpawnPrefabDataframe f = (SpawnPrefabDataframe)frame;
                                                                    repl._presentlyReplicatingSceneObject = f.PrefabPath;
                                                                    channel.SendPacket(new SpawnPrefabPacket()
                                                                    {
                                                                        Room = room,
                                                                        ScenePath = scene,

                                                                        PrefabPath = f.PrefabPath,
                                                                        ParentObjectPath = f.ObjectPath
                                                                    });
                                                                    break;
                                                                }
                                                        }
                                                    }
                                                }
                                            }
                                            finally
                                            {
                                                if (!first)
                                                    channel.SendPacket(new SceneReplicationCompletePacket()
                                                    {
                                                        Room = room,
                                                        ScenePath = scene
                                                    });
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    // Finish
                                    repl._presentlyReplicatingScene = null;
                                    repl._presentlyReplicatingSceneObject = null;
                                    repl._presentlyReplicatingRoom = null;
                                    repl._replicating = false;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // Exception
                        if (conn.IsConnected() && Game.DebugMode)
                            Logger.GetLogger("scene-manager").Error("Scene replication error! Note that this might be a rough disconenct, this message wont show outside of debug mode.", e);
                    }
                }
            }

            // Finish
            _replicating = false;
        }

    }
}