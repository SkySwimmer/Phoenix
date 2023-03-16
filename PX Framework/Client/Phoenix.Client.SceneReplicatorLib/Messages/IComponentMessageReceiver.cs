using Phoenix.Common.SceneReplication.Messages;

namespace Phoenix.Client.SceneReplicatorLib.Messages
{
    /// <summary>
    /// Interface for handling component messages on the client
    /// </summary>
    public interface IComponentMessageReceiver
    {
        /// <summary>
        /// Messenger instances for each room
        /// </summary>
        public Dictionary<string, ComponentMessenger> Messengers { get; set; }

        /// <summary>
        /// Handles component messages
        /// </summary>
        /// <param name="message">Message to handle</param>
        /// <param name="messenger">Messenger that received the message, attached to a specific room, use this to reply</param>
        public void HandleMessage(IComponentMessage message, ComponentMessenger messenger);

        /// <summary>
        /// Called when replication is set up and passes the messenger instance to the component, here you can register your messages
        /// </summary>
        /// <param name="messenger">Component messenger instance</param>
        public void SetupMessenger(ComponentMessenger messenger);
    }
}
