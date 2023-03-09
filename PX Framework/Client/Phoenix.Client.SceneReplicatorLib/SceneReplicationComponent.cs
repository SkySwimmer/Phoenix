using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Client.SceneReplicatorLib.Handlers.InitialSync;
using Phoenix.Client.SceneReplicatorLib.Handlers.Replication;
using Phoenix.Client.SceneReplicatorLib.Handlers.SceneLoading;
using Phoenix.Client.SceneReplicatorLib.Handlers.Subscription;
using Phoenix.Common.SceneReplication;
using System.Reflection;

namespace Phoenix.Client.Components
{
    /// <summary>
    /// Client-side Scene Replication Component - Controls the scene replication system (<b>note: this requires a engine-specific binding library</b>)
    /// </summary>
    public class SceneReplicationComponent : ClientComponent
    {
        public override string ID => "scene-replication";

        private List<string> _subscribedScenes = new List<string>();
        private List<string> _subscribedRooms = new List<string>();
        private List<string> _loadedScenes = new List<string>();
        private List<string> _loadingScenes = new List<string>();

        /// <summary>
        /// Retrieves the active binding library (may return null)
        /// </summary>
        public SceneReplicationBindings? Bindings;

        internal void CompleteSubscribeScene(string room, string scene)
        {
            if (!IsSubscribedToRoom(room))
                _subscribedRooms.Add(room);
            if (!IsSubscribedToScene(scene))
            {
                GetLogger().Debug("Subscribed to scene replication scene: " + scene + " in room " + room);
                _subscribedScenes.Add(scene);
            }
        }

        internal void SubscribeRoom(string room)
        {
            if (!IsSubscribedToRoom(room))
            {
                GetLogger().Debug("Subscribed to scene replication room: " + room);
                _subscribedRooms.Add(room);
            }
        }

        internal void DesbuscribeRoom(string room)
        {
            if (IsSubscribedToRoom(room))
            {
                GetLogger().Debug("Desubscribed from scene replication room: " + room);
                _subscribedRooms.Remove(room);
            }
        }

        internal void DesubscribeScene(string scene)
        {
            if (IsSubscribedToScene(scene))
            {
                GetLogger().Debug("Desubscribed from scene replication scene: " + scene);
                _subscribedScenes.Remove(scene);
            }
        }

        /// <summary>
        /// Marks the given scene path as loading so that subscription events wont fail and instead will wait for loading to finish
        /// </summary>
        /// <param name="scene">Scene path</param>
        public void BeginLoadingScene(string scene)
        {
            if (!IsSceneLoaded(scene) && !IsSceneLoading(scene))
                _loadingScenes.Add(scene);
        }

        /// <summary>
        /// Cancels scene loading, marking all in-progress subscription events for this scene as failed
        /// </summary>
        /// <param name="scene">Scene path</param>
        public void CancelLoadingScene(string scene)
        {
            if (IsSceneLoading(scene))
                _loadingScenes.Remove(scene);
        }

        /// <summary>
        /// Marks the given scene as loaded, marking all in-progress subscription events for this scene as successful
        /// </summary>
        /// <param name="scene">Scene path</param>
        public void FinishLoadingScene(string scene)
        {
            if (!IsSceneLoaded(scene))
                _loadedScenes.Add(scene);
            if (IsSceneLoading(scene))
                _loadingScenes.Remove(scene);
        }

        /// <summary>
        /// Unloads scenes from memory, this will mark any subscription events to this scene as failed
        /// </summary>
        /// <param name="scene">Scene path</param>
        public void UnloadScene(string scene)
        {
            if (IsSceneLoaded(scene))
                _loadedScenes.Remove(scene);
        }

        /// <summary>
        /// Checks if a scene is known to be loaded
        /// </summary>
        /// <param name="scene">Scene path</param>
        /// <returns>True if loaded, false otherwise</returns>
        public bool IsSceneLoaded(string scene)
        {
            return _loadedScenes.Contains(scene);
        }

        /// <summary>
        /// Checks if a scene is known to be presently loading
        /// </summary>
        /// <param name="scene">Scene path</param>
        /// <returns>True if the scene is loading, false otherwise</returns>
        public bool IsSceneLoading(string scene)
        {
            return _loadingScenes.Contains(scene);
        }

        /// <summary>
        /// Checks if the client is subscribed to a specific room
        /// </summary>
        /// <param name="room">Room ID</param>
        /// <returns>True if subscribed, false otherwise</returns>
        public bool IsSubscribedToRoom(string room)
        {
            return _subscribedRooms.Contains(room);
        }

        /// <summary>
        /// Checks if the client is subscribed to a specific scee
        /// </summary>
        /// <param name="scene">Scene path</param>
        /// <returns>True if subscribed, false otherwise</returns>
        public bool IsSubscribedToScene(string room)
        {
            return _subscribedScenes.Contains(room);
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
        /// Retrieves the rooms the client is subscribed to
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

        protected override void Define()
        {
            // Some binding libraries might depend on this
            DependsOn("task-manager");
        }

        public override void PreInit()
        {
            // Register packet handlers
            SceneReplicationChannel channel;
            try
            {
                channel = Client.ChannelRegistry.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the client packet registry.");
            }

            // Scene loading
            channel.RegisterHandler(new LoadSceneHandler());
            channel.RegisterHandler(new UnloadSceneHandler());

            // Initial sync
            /*channel.RegisterHandler(new InitialSceneReplicationStartHandler());
            channel.RegisterHandler(new InitialSceneReplicationCompleteHandler());*/

            // Subscription
            channel.RegisterHandler(new SceneReplicationSubscribeRoomHandler());
            channel.RegisterHandler(new SceneReplicationSubscribeSceneHandler());
            channel.RegisterHandler(new SceneReplicationDesubscribeRoomHandler());
            channel.RegisterHandler(new SceneReplicationDesubscribeSceneHandler());

            // Replication
            /*channel.RegisterHandler(new SceneReplicationStartHandler());
            channel.RegisterHandler(new SceneReplicationCompleteHandler());
            channel.RegisterHandler(new SpawnPrefabHandler());
            channel.RegisterHandler(new DestroyObjectHandler());
            channel.RegisterHandler(new ReparentObjectHandler());
            channel.RegisterHandler(new ObjectChangeSceneHandler());
            channel.RegisterHandler(new ReplicateObjectHandler());*/

            if (Bindings == null)
            {
                // Search for a binding library
                GetLogger().Info("Searching for binding libraries...");
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type t in asm.GetTypes())
                        {
                            if (typeof(SceneReplicationBindings).IsAssignableFrom(t))
                            {
                                try
                                {
                                    Bindings = (SceneReplicationBindings)t.GetConstructor(new Type[] { typeof(SceneReplicationComponent) }).Invoke(new object[] { this });
                                    GetLogger().Info("Selected binding library: " + Bindings.GetName());
                                    break;
                                }
                                catch { }
                            }
                        }
                        if (Bindings != null)
                            break;
                    }
                    catch { }
                }
                if (Bindings == null)
                {
                    // Log warning
                    GetLogger().Warn("No binding library present! Please add a binding library to the project, else scene replication will not work!");
                }
            }
        }
    }
}
