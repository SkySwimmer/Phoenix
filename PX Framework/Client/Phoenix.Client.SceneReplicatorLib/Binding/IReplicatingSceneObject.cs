using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Binding
{
    /// <summary>
    /// Replicating scene object interface
    /// </summary>
    public interface IReplicatingSceneObject
    {
        /// <summary>
        /// Called to destroy the object
        /// <br/>
        /// <br/>
        /// Typically called from the engine's engine update.
        /// </summary>
        public void Destroy();

        /// <summary>
        /// Called to apply replicated changes
        /// <br/>
        /// <br/>
        /// Typically called from the engine's engine update.
        /// </summary>
        /// <param name="packet">Packet containing the changes</param>
        public void Replicate(ReplicateObjectPacket packet);

        /// <summary>
        /// Called to change the parent object
        /// <br/>
        /// <br/>
        /// Typically called from the engine's engine update.
        /// </summary>
        /// <param name="newParent">New parent object path or null if it should be reparented to the root</param>
        public void Reparent(string? newParent);

        /// <summary>
        /// Called to change the object scene
        /// <br/>
        /// <br/>
        /// Typically called from the engine's engine update.
        /// </summary>
        /// <param name="newScene">New scene path</param>
        public void ChangeScene(string newScene);
    }
}
