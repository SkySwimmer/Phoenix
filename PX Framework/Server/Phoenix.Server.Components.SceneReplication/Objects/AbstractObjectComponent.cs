using Phoenix.Common.Networking.Connections;
using Phoenix.Server.SceneReplication.Coordinates;
using Phoenix.Server.SceneReplication.Data;

namespace Phoenix.Server.SceneReplication
{
    /// <summary>
    /// Abstract object component
    /// </summary>
    public abstract class AbstractObjectComponent : SerializingObject
    {
        private bool _inited;
        private SceneObject _object;
        private Connection? _owningConnection;

        /// <summary>
        /// Retrieves the owning connection of this object (assigning this will prevent other clients from receiving and sending messages to this component)
        /// </summary>
        public Connection? OwningConnection
        {
            get
            {
                if (_owningConnection != null)
                    return _owningConnection;
                return SceneObject.OwningConnection;
            }

            set
            {
                if (_owningConnection != null)
                    _owningConnection.Disconnected -= DisconnectHandler;
                _owningConnection = value;
                if (_owningConnection != null)
                    _owningConnection.Disconnected += DisconnectHandler;
            }
        }

        private void DisconnectHandler(Connection connection, string reason, string[] args)
        {
            Disconnect(reason, args);
        }

        /// <summary>
        /// Retrieves the owning scene object
        /// </summary>
        public SceneObject SceneObject
        {
            get
            {
                return _object;
            }
        }

        /// <summary>
        /// Retrieves the object transform
        /// </summary>
        public Transform Transform
        {
            get
            {
                return _object.Transform;
            }
        }

        internal void Setup(SceneObject owner)
        {
            _object = owner;

            // Check object
            if (owner.Scene != null)
                PerformInit();
        }

        internal void PerformInit()
        {
            if (!_inited)
                Init();
            _inited = true;
            Start();
            if (_object.Active)
                Enable();
        }

        /// <summary>
        /// Called when the component is first initialized (only called the first time when the object is added to a scene)
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// Called on component start (called each time the object is added to a scene)
        /// </summary>
        public virtual void Start() { }

        /// <summary>
        /// Called each time the object is changed to active
        /// </summary>
        public virtual void Enable() { }

        /// <summary>
        /// Called on component update
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Called each time the object is changed to inactive
        /// </summary>
        public virtual void Disable() { }

        /// <summary>
        /// Called on component stop (called each time the object is removed from a scene)
        /// </summary>
        public virtual void Stop() { }

        /// <summary>
        /// Called when the object is destroyed or when the component is removed from the object
        /// </summary>
        public virtual void Destroy() { }

        /// <inheritdoc/>
        public virtual void Deserialize(Dictionary<string, object> data) { }

        /// <inheritdoc/>
        public virtual void Serialize(Dictionary<string, object> data) { }

        /// <summary>
        /// Called when the owning connection disconnects
        /// </summary>
        /// <param name="reason">Disconnect reason</param>
        /// <param name="args">Disconnect arguments</param>
        public virtual void Disconnect(string reason, string[] args) { }
    }
}
