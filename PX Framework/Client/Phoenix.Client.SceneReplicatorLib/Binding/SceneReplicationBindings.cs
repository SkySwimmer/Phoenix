using Phoenix.Client.SceneReplicatorLib.Messages;
using Phoenix.Common.SceneReplication.Packets;
using static Phoenix.Common.SceneReplication.Packets.InitialSceneReplicationStartPacket;

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
        /// Schedules an action to run on the next engine update
        /// </summary>
        /// <param name="action">Action to schedule</param>
        public abstract void RunOnNextFrameUpdate(Action action);

        /// <summary>
        /// Called to retrieve scene objects (should return null if the object does not replicate)
        /// <br/>
        /// <br/>
        /// Typically called from the engine's engine update.
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        /// <param name="objectID">Object ID string</param>
        /// <returns>IReplicatingSceneObject instance or null</returns>
        public abstract IReplicatingSceneObject? GetObjectByIDInScene(string room, string scenePath, string objectID);

        /// <summary>
        /// Retrieves the networked components of a object
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        /// <param name="objectID">Object ID string</param>
        /// <returns>Array of IComponentMessageReceiver instances</returns>
        public abstract IComponentMessageReceiver[] GetNetworkedComponents(string room, string scenePath, string objectID);

        /// <summary>
        /// Retrieves the object path of a object
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        /// <param name="objectID">Object ID string</param>
        /// <returns>Object path string</returns>
        public abstract string GetObjectPathByID(string room, string scenePath, string objectID);

        /// <summary>
        /// Called to spawn prefabs
        /// <br/>
        /// <br/>
        /// Typically called from the engine's engine update.
        /// </summary>
        /// <param name="packet">Prefab information packet</param>
        public abstract void SpawnPrefab(SpawnPrefabPacket packet);

        /// <summary>
        /// Called to spawn empty objects
        /// <br/>
        /// <br/>
        /// Typically called from the engine's engine update.
        /// </summary>
        /// <param name="packet">Prefab information packet</param>
        public abstract void CreateObject(CreateObjectPacket packet);

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
        /// WARNING! This is called from the engine's engine update! Please perform event dispatching on another thread as blocking will cause lag spikes in the game!
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        /// <param name="objectMap">Scene object map containing object IDs</param>
        public abstract void OnBeginInitialSync(string room, string scenePath, Dictionary<string, SceneObjectID> objectMap);

        /// <summary>
        /// Called when the server finishes initial scene replication
        /// <br/>
        /// <br/>
        /// WARNING! This is called from the engine's engine update! Please perform event dispatching on another thread as blocking will cause lag spikes in the game!
        /// </summary>
        /// <param name="room">Replication room</param>
        /// <param name="scenePath">Scene path</param>
        public abstract void OnFinishInitialSync(string room, string scenePath);
    }
}
