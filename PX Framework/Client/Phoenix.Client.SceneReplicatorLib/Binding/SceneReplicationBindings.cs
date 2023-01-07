using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Binding
{
    /// <summary>
    /// Abstract class for Scene Replication Binding Libraries
    /// </summary>
    public abstract class SceneReplicationBindings
    {
        /// <summary>
        /// Retrieves the binding library name
        /// </summary>
        /// <returns>Binding library name</returns>
        public abstract string GetName();

        /// <summary>
        /// Schedules an action to run on the next frame update
        /// </summary>
        /// <param name="action">Action to schedule</param>
        public abstract void RunOnNextFrameUpdate(Action action);

        /// <summary>
        /// Called to retrieve scene objects (should return null if the object does not replicate)
        /// <br/>
        /// <br/>
        /// Typically called from the engine's frame update.
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        /// <param name="objectPath"></param>
        /// <returns>IReplicatingSceneObject instance or null</returns>
        public abstract IReplicatingSceneObject? GetObjectInScene(string room, string scenePath, string objectPath);

        /// <summary>
        /// Called to spawn prefabs
        /// <br/>
        /// <br/>
        /// Typically called from the engine's frame update.
        /// </summary>
        /// <param name="packet">Prefab information packet</param>
        public abstract void SpawnPrefab(SpawnPrefabPacket packet);

        /// <summary>
        /// Called to load a scene
        /// <br/>
        /// <br/>
        /// WARNING! This is called synchronously on the client thread! After performing component interactions please do the actual loading on a different thread.
        /// </summary>
        /// <param name="scenePath">Scene path</param>
        /// <param name="additive">True if the server requests 'additive' loading (keeping other scenes open), false if the server requests 'single' scene loading (closing other scenes)</param>
        public abstract void LoadScene(string scenePath, bool additive);

        /// <summary>
        /// Called to unload a scene
        /// <br/>
        /// <br/>
        /// WARNING! This is called synchronously on the client thread! After performing component interactions please do the actual unloading process on a different thread.
        /// </summary>
        /// <param name="scenePath">Scene path</param>
        public abstract void UnloadScene(string scenePath);

        /// <summary>
        /// Called when the server is about to begin initial scene replication
        /// <br/>
        /// <br/>
        /// WARNING! This is called from the engine's frame update! Please perform event dispatching on another thread as blocking will cause lag spikes in the game!
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        public abstract void OnBeginInitialSync(string room, string scenePath);

        /// <summary>
        /// Called when the server finishes initial scene replication
        /// <br/>
        /// <br/>
        /// WARNING! This is called from the engine's frame update! Please perform event dispatching on another thread as blocking will cause lag spikes in the game!
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        public abstract void OnFinishInitialSync(string room, string scenePath);
    }
}
