namespace Phoenix.Common.SceneReplication.Messages
{
    /// <summary>
    /// Simple message sender delegate, used to send reply messages
    /// </summary>
    /// <param name="msg">Message to send</param>
    public delegate void ComponentMessageSender(IComponentMessage msg);

    /// <summary>
    /// Marks methods in components as a message handler, handler methods should have two parameters, namely the IComponentMessage instance it should handle (this compares types) and the ComponentMessageSender instance to send replies through, on the client the second parameter can be replaced with a ComponentMessenger to receive the messenger instance of the room.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MessageHandlerAttribute : Attribute
    {
    }
}
