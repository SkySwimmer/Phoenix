using Phoenix.Common.SceneReplication.Data;

namespace Phoenix.Common.SceneReplication.Messages
{
    /// <summary>
    /// Component message interface
    /// </summary>
    public interface IComponentMessage : SerializingObject
    {
        /// <summary>
        /// Message ID string
        /// </summary>
        public string MessageID { get; }

        /// <summary>
        /// Creates a instance of this componetn message
        /// </summary>
        /// <returns>New IComponentMessage instance</returns>
        public IComponentMessage CreateInstance();
    }
}
