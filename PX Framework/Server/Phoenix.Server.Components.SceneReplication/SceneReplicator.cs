using Phoenix.Common.Networking.Connections;
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

        internal string? _presentlyReplicatingRoom = null;
        internal string? _presentlyReplicatingSceneObject = null;
        internal string? _presentlyReplicatingScene = null;
        internal bool _replicating = false;

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
        /// Retrieves the scene that is currently being transferred to the clients
        /// </summary>
        public string? PresentlyReplicatingScene
        {
            get
            {
                return _presentlyReplicatingScene;
            }
        }

        /// <summary>
        /// Retrieves the scene object that is currently being transferred to the clients
        /// </summary>
        public string? PresentlyReplicatingSceneObject
        {
            get
            {
                return _presentlyReplicatingSceneObject;
            }
        }

        /// <summary>
        /// Retrieves the room that is currently being transferred to the clients
        /// </summary>
        public string? PresentlyReplicatingRoom
        {
            get
            {
                return _presentlyReplicatingRoom;
            }
        }


        public SceneReplicator(Connection client, SceneManager manager)
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
            channel.SendPacket(new LoadScenePacket() { 
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

            // Wait for replication to finish
            while (_replicating)
                Thread.Sleep(1);

            // Stop the main replicator until the initial sync is done
            _replicating = true;

            // Send initial sync packet
            channel.SendPacket(new InitialSceneReplicationStartPacket()
            {
                ScenePath = sc.Path,
                Room = room
            });

            #region Replication

            // Send start
            channel.SendPacket(new SceneReplicationStartPacket()
            {
                ScenePath = sc.Path,
                Room = room
            });

            // Send objects that were destroyed
            foreach (string destroyed in sc.DestroyedObjects)
            {
                channel.SendPacket(new DestroyObjectPacket() { 
                    ObjectPath = destroyed,
                    ScenePath = sc.Path,
                    Room = room
                });
            }

            // Send objects that were moved to a different scene
            foreach (string moved in sc.NewObjectScenes)
            {
                while (true)
                {
                    try
                    {
                        if (sc._newObjectScenes.ContainsKey(moved))
                            channel.SendPacket(new ObjectChangeScenePacket()
                            {
                                NewScenePath = sc._newObjectScenes[moved],
                                ObjectPath = moved,
                                ScenePath = sc.Path,
                                Room = room
                            });
                        break;
                    }
                    catch { }
                }
            }

            // Send objects that were reparented
            foreach (string reparented in sc.ReparentedObjects)
            {
                while (true)
                {
                    try
                    {
                        if (sc._reparentedObjects.ContainsKey(reparented))
                        {
                            SceneObject? obj = sc._reparentedObjects[reparented];
                            channel.SendPacket(new ReparentObjectPacket()
                            {
                                NewParentPath = obj == null ? null : obj.Path,
                                ObjectPath = reparented,
                                ScenePath = sc.Path,
                                Room = room
                            });
                        }
                        break;
                    }
                    catch { }
                }
            }

            // Send prefabs
            foreach (string prefab in sc.SpawnedPrefabs)
            {
                while (true)
                {
                    try
                    {
                        if (sc._spawnedPrefabs.ContainsKey(prefab))
                        {
                            channel.SendPacket(new SpawnPrefabPacket()
                            {
                                ParentObjectPath = sc._spawnedPrefabs[prefab],
                                PrefabPath = prefab,
                                ScenePath = sc.Path,
                                Room = room
                            });
                        }
                        break;
                    }
                    catch { }
                }
            }

            // Send replication data
            void SyncObj(SceneObject obj)
            {
                if (obj.Replicates)
                {
                    channel.SendPacket(new ReplicateObjectPacket()
                    {
                        HasActiveStatusChanges = true,
                        HasDataChanges = true,
                        HasTransformChanges = true,
                        HasNameChanges = true,

                        Name = obj.Name,
                        Active = obj.Active,
                        Data = obj.ReplicationData.data,
                        Transform = obj.Transform.ToPacketTransform(),

                        ObjectPath = obj.Path,
                        ScenePath = sc.Path,
                        Room = room
                    });
                }
                foreach (SceneObject ch in obj.Children)
                    SyncObj(ch);
            }
            foreach (SceneObject obj in sc.Objects)
            {
                SyncObj(obj);
            }

            // Send finish
            channel.SendPacket(new SceneReplicationCompletePacket()
            {
                ScenePath = sc.Path,
                Room = room
            });
            
            #endregion Replication

            // Send sync complete packet
            channel.SendPacket(new InitialSceneReplicationCompletePacket()
            {
                ScenePath = sc.Path,
                Room = room
            });

            // Resume main replicator
            _replicating = false;
        }
    }
}
