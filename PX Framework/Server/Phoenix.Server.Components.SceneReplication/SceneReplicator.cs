using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Server.SceneReplication
{
    public enum SceneLoadMethod
    {
        SINGLE, ADDITIVE
    }

    /// <summary>
    /// Scene Replication Controller
    /// </summary>
    public class SceneReplicator
    {
        private Connection _client;
        private SceneManager _manager;
        private List<string> _subscribedRooms = new List<string>();
        private List<string> _subscribedScenes = new List<string>();
        private List<string> _scenesAwaitingSubscription = new List<string>();

        private List<AbstractNetworkPacket> _replicationPackets = new List<AbstractNetworkPacket>();
        private bool _replicationReady;

        internal void SendReplicationPacket(AbstractNetworkPacket packet)
        {
            if (!_replicationReady)
            {
                // Queue
                lock(_replicationPackets)
                    _replicationPackets.Add(packet);
                return;
            }

            // Send sync
            SceneReplicationChannel channel;
            try
            {
                channel = _client.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                return;
            }
            channel.SendPacket(packet);
        }

        internal SceneReplicator(Connection client, SceneManager manager)
        {
            _client = client;
            _manager = manager;

            // Get channel
            SceneReplicationChannel channel;
            try
            {
                channel = _client.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }

            // Attach subscribe response handler
            SceneSubscribeHandler? handler = channel.GetHandlerDefinition<SceneSubscribeHandler>();
            if (handler != null)
            {
                handler.Handler = packet =>
                {
                    // Check room and scene
                    if (IsSubscribedToRoom(packet.Room) && IsScenePendingSubscription(packet.ScenePath))
                    {
                        // Handle it
                        if (packet.Success)
                        {
                            // Subscribe success
                            _subscribedScenes.Add(packet.ScenePath);
                            _scenesAwaitingSubscription.Remove(packet.ScenePath);

                            // Perform sync
                            SyncRoom(_manager.GetScene(packet.ScenePath, packet.Room), packet.Room);
                        }
                        else
                        {
                            // Subscribe failure
                            _scenesAwaitingSubscription.Remove(packet.ScenePath);
                        }
                    }
                    return true;
                };
            }
        }

        /// <summary>
        /// Retrieves the room IDs the client is subscribed to
        /// </summary>
        public string[] SubscribedRooms
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _subscribedRooms.ToArray();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Retrieves the scenes the client is subscribed to
        /// </summary>
        public string[] SubscribedScenes
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _subscribedScenes.ToArray();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Retrieves the scenes the client needs to subscribe to
        /// </summary>
        public string[] ScenesPendingSubscription
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _scenesAwaitingSubscription.ToArray();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Checks if the replicator is subscribed to a specific room
        /// </summary>
        /// <param name="room">Room ID</param>
        /// <returns>True if subscribed, false otherwise</returns>
        public bool IsSubscribedToRoom(string room)
        {
            return _subscribedRooms.Contains(room);
        }

        /// <summary>
        /// Checks if the replicator is subscribed to a specific scene
        /// </summary>
        /// <param name="scene">Scene ID</param>
        /// <returns>True if subscribed, false otherwise</returns>
        public bool IsSubscribedToScene(string scene)
        {
            return _subscribedScenes.Contains(scene);
        }

        /// <summary>
        /// Checks if the replicator is awaiting subscription for a specific scene
        /// </summary>
        /// <param name="scene">Scene ID</param>
        /// <returns>True if the replicator is awaiting a client response, false otherwise</returns>
        public bool IsScenePendingSubscription(string scene)
        {
            return _scenesAwaitingSubscription.Contains(scene);
        }

        /// <summary>
        /// Desubscribes from a room
        /// </summary>
        /// <param name="room">Room ID to desubscribe from</param>
        public void DesubscribeFromRoom(string room)
        {
            if (!IsSubscribedToRoom(room))
                throw new ArgumentException("Invalid room");
            _subscribedRooms.Remove(room);

            // Send desubscribe command
            SceneReplicationChannel channel;
            try
            {
                channel = _client.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }
            channel.SendPacket(new SceneReplicationDesubscribeRoomPacket()
            {
                Room = room
            });

            // Desubscribe all scenes if not subscribed to any room
            if (SubscribedRooms.Length == 0)
            {
                foreach (string sceneStr in SubscribedScenes)
                    DesubscribeFromScene(sceneStr);
            }
        }

        /// <summary>
        /// Desubscribes from a scene
        /// </summary>
        /// <param name="scene">Scene to desubscribe from</param>
        public void DesubscribeFromScene(string scene)
        {
            if (!IsSubscribedToScene(scene))
                throw new ArgumentException("Invalid scene");
            _subscribedScenes.Remove(scene);

            // Send desubscribe command
            SceneReplicationChannel channel;
            try
            {
                channel = _client.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }
            channel.SendPacket(new SceneReplicationDesubscribeScenePacket()
            {
                ScenePath = scene
            });

            // Desubscribe from all rooms if not subscribed to any scene
            if (SubscribedScenes.Length == 0)
            {
                foreach (string room in SubscribedRooms)
                    DesubscribeFromRoom(room);
            }
        }

        /// <summary>
        /// Subscribes to a scene and room, sending over all room objects when the client replies with success
        /// </summary>
        /// <param name="scene">Scene asset path (without .prsm, reads from subdirectory SceneReplication, eg. <u>Scenes/SampleScene</u>)</param>
        /// <param name="room">Room ID</param>
        /// <returns>Scene instance</returns>
        public Scene SubscribeScene(string scene, string room = "DEFAULT")
        {
            Scene sc = _manager.GetScene(scene, room);

            // Get channel
            SceneReplicationChannel channel;
            try
            {
                channel = _client.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }

            // Check subscription status
            if (IsSubscribedToScene(scene) || IsScenePendingSubscription(scene))
            {
                if (!IsSubscribedToRoom(room))
                {
                    if (!_manager.RoomExists(room))
                        throw new ArgumentException("Invalid room");
                    _subscribedRooms.Add(room);

                    // Send subscribe command
                    channel.SendPacket(new SceneReplicationSubscribeRoomPacket()
                    {
                        Room = room
                    });

                    // Send subscribe command
                    channel.SendPacket(new SceneReplicationSubscribeScenePacket()
                    {
                        Room = room,
                        ScenePath = scene
                    });

                    return sc;
                }
                if (IsScenePendingSubscription(scene))
                    throw new ArgumentException("Subscription process is already in progress for '" + scene + "'");
                else
                    throw new ArgumentException("Already subscribed to '" + scene + "' in room '" + room + "'");
            }

            // Subscribe to room
            if (!IsSubscribedToRoom(room))
            {
                _subscribedRooms.Add(room);

                // Send subscribe command
                channel.SendPacket(new SceneReplicationSubscribeRoomPacket()
                {
                    Room = room
                });
            }

            // Set pending subscription
            if (!IsScenePendingSubscription(scene))
                _scenesAwaitingSubscription.Add(scene);

            // Send subscribe command
            channel.SendPacket(new SceneReplicationSubscribeScenePacket()
            {
                Room = room,
                ScenePath = scene
            });

            // 
            // For those curious what happens next
            // 
            // The server-side subscription chain is done, it is now up to the client
            // When the client responds, if successful, the server will send over all objects
            // This happens asynchronous to the subscription calls
            //
            // By doing it this way we ensure that the client is ready to handle scene replication when the server
            // sends objects over. This way it wont need to keep track of scene replication packets until its scenes are loaded.
            //

            return sc;
        }

        /// <summary>
        /// Makes the client load a scene and subscribes to it, sending over all room objects when the client replies with success
        /// </summary>
        /// <param name="scene">Scene asset path (without .prsm, reads from subdirectory SceneReplication, eg. <u>Scenes/SampleScene</u>)</param>
        /// <param name="room">Room ID</param>
        /// <param name="loadMethod">Scene load method</param>
        /// <returns>Scene instance</returns>
        public Scene LoadScene(string scene, string room = "DEFAULT", SceneLoadMethod loadMethod = SceneLoadMethod.SINGLE)
        {
            Scene sc = _manager.GetScene(scene, room);

            // Send load command
            SceneReplicationChannel channel;
            try
            {
                channel = _client.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }
            channel.SendPacket(new LoadScenePacket()
            {
                ScenePath = sc.Path,
                Additive = loadMethod == SceneLoadMethod.ADDITIVE
            });

            // Subscribe to scene
            if (!IsSubscribedToRoom(room) || !IsSubscribedToScene(scene))
                SubscribeScene(scene, room);
            return sc;
        }

        /// <summary>
        /// Unloads and desubscribes from a scene
        /// </summary>
        /// <param name="scene">Scene asset path (without .prsm, reads from subdirectory SceneReplication, eg. <u>Scenes/SampleScene</u>)</param>
        public void UnloadScene(string scene)
        {
            // Send unload command
            SceneReplicationChannel channel;
            try
            {
                channel = _client.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }
            channel.SendPacket(new UnloadScenePacket()
            {
                ScenePath = scene
            });

            if (IsSubscribedToScene(scene))
                DesubscribeFromScene(scene);
        }

        internal void SyncRoom(Scene sc, string room)
        {
            // Retrieve packet channel
            SceneReplicationChannel channel;
            try
            {
                channel = _client.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }

            // Lock replicator
            _replicationReady = false;

            // Send initial sync packet
            channel.SendPacket(new InitialSceneReplicationStartPacket()
            {
                ScenePath = sc.Path,
                Room = room,
                ObjectMap = _manager._sceneObjectMaps[sc]
            });

            #region Replication

            // Send destroyed objects
            List<string> objIds;
            lock (_manager._destroyedObjects)
                objIds = _manager._destroyedObjects[sc];
            foreach (string id in objIds)
            {
                // Send packet
                channel.SendPacket(new DestroyObjectPacket()
                {
                    ScenePath = sc.Path,
                    Room = room,
                    ObjectID = id
                });
            }

            // Send scene changes
            List<SceneObject> objs;
            lock (_manager._sceneSwitchedObjects)
                objs = _manager._sceneSwitchedObjects[sc];
            foreach (SceneObject obj in objs)
            {
                // Send packet
                channel.SendPacket(new ObjectChangeScenePacket()
                {
                    ScenePath = sc.Path,
                    Room = room,

                    ObjectID = obj.ID,
                    NewScenePath = obj.Scene == null ? null : obj.Scene.Path
                });
            }

            // Send reparented objects
            lock (_manager._reparentedObjects)
                objs = _manager._reparentedObjects[sc];
            foreach (SceneObject obj in objs)
            {
                // Send packet
                channel.SendPacket(new ReparentObjectPacket()
                {
                    ScenePath = sc.Path,
                    Room = room,

                    ObjectID = obj.ID,
                    NewParentID = obj.Parent == null ? null : obj.Parent.ID
                });
            }

            // Send spawned prefabs
            Dictionary<SceneObject, string> prefabs;
            lock (_manager._spawnedPrefabs)
                prefabs = _manager._spawnedPrefabs[sc];
            List<string> replicatingPrefabs = new List<string>();
            void replicatePrefab(SceneObject prefab)
            {
                if (replicatingPrefabs.Contains(prefab.ID))
                    return;
                replicatingPrefabs.Add(prefab.ID);

                // Replicate parent prefab first if its a prefab
                if (prefab.Parent != null)
                    replicatePrefab(prefab.Parent);
                if (!prefabs.ContainsKey(prefab))
                    return; // Not a prefab, prob a parent object

                // Replicate prefab
                channel.SendPacket(new SpawnPrefabPacket()
                {
                    ScenePath = sc.Path,
                    Room = room,
                    
                    PrefabPath = prefabs[prefab],
                    ObjectID = prefab.ID,
                    ParentObjectID = prefab.Parent == null ? null : prefab.Parent.ID
                });
            }
            foreach (SceneObject prefab in prefabs.Keys)
            {
                replicatePrefab(prefab);
            }

            // Replicate fields
            lock (_manager._editedSceneObjets)
                objs = _manager._editedSceneObjets[sc];
            foreach (SceneObject obj in objs)
            {
                // Send packet
                channel.SendPacket(new ReplicateObjectPacket()
                {
                    ScenePath = sc.Path,
                    Room = room,

                    ObjectID = obj.ID,

                    HasActiveStatusChanges = true,
                    HasDataChanges = true,
                    HasNameChanges = true,
                    HasTransformChanges = true,
                    IsInitial = true,

                    Active = obj.Active,
                    Data = obj.ReplicationData.data,
                    Name = obj.Name,
                    Transform = obj.Transform.ToPacketTransform()
                });
            }

            #endregion Replication

            // Send sync complete packet
            channel.SendPacket(new InitialSceneReplicationCompletePacket()
            {
                ScenePath = sc.Path,
                Room = room
            });

            // Resume main replicator
            _replicationReady = true;
            while (_replicationPackets.Count != 0)
            {
                // Perform sync
                AbstractNetworkPacket[] packets;
                lock (_replicationPackets)
                {
                    packets = _replicationPackets.ToArray();
                    _replicationPackets.Clear();
                }
                foreach (AbstractNetworkPacket pkt in packets)
                    channel.SendPacket(pkt);
            }
        }
    }
}
