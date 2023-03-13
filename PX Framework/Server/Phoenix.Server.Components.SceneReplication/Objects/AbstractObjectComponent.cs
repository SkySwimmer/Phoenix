using Phoenix.Common;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.SceneReplication;
using Phoenix.Common.SceneReplication.Data;
using Phoenix.Common.SceneReplication.Messages;
using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Server.SceneReplication.Coordinates;
using Phoenix.Server.SceneReplication.Data;
using Transform = Phoenix.Server.SceneReplication.Coordinates.Transform;

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

        private GameServer _server;
        private string _room;

        private bool inited;
        internal List<IComponentMessage> _messageRegistry = new List<IComponentMessage>();

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

        internal void SetupNetwork(GameServer server, string room)
        {
            if (inited)
                return;
            RegisterMessages();
            _server = server;
            _room = room;
            inited = true;
        }

        /// <summary>
        /// Called to register component messages
        /// </summary>
        protected virtual void RegisterMessages() { }

        internal ComponentMessageSender InternalReplySender(Connection origin)
        {
            return message =>
            {
                if (!_messageRegistry.Any(t => t.MessageID == message.MessageID))
                    throw new ArgumentException("Message not registered");
                if (SceneObject.Scene == null)
                    throw new ArgumentException("Not in any scene");

                // Create packet
                ComponentMessagePacket pkt = new ComponentMessagePacket();
                pkt.ObjectID = SceneObject.ID;
                pkt.ScenePath = SceneObject.Scene.Path;
                pkt.MessengerComponentIndex = Array.IndexOf(SceneObject.Components, this);
                pkt.Room = _room;
                if (Game.DebugMode)
                {
                    // Add debug information                
                    pkt.HasDebugHeaders = true;
                    pkt.DebugRemoteComponentTypeName = GetType().FullName;
                    pkt.DebugComponentMessageRegistry = new Dictionary<string, int>();

                    // Add registry
                    int index = 0;
                    foreach (IComponentMessage msg in _messageRegistry)
                        pkt.DebugComponentMessageRegistry[msg.MessageID] = index++;
                }
                pkt.MessageID = _messageRegistry.FindIndex(t => t.MessageID == message.MessageID);
                pkt.MessagePayload = SerializingObjects.SerializeObject(message);

                // Send message
                origin.GetChannel<SceneReplicationChannel>().SendPacket(pkt);
            };
        }

        internal void HandleMessageInternal(IComponentMessage msg, Connection origin)
        {
            HandleMessage(msg, InternalReplySender(origin));
        }

        /// <summary>
        /// Registers component messages
        /// </summary>
        /// <param name="message">Component message to register</param>
        protected void RegisterMessage(IComponentMessage message)
        {
            if (inited)
                throw new ArgumentException("Registry locked");
            if (_messageRegistry.Any(t => t.MessageID == message.MessageID))
                throw new ArgumentException("Message already registered or another exists with the same ID");
            _messageRegistry.Add(message);
        }

        /// <summary>
        /// Sends messages to clients
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="message">Message instance</param>
        public void SendMessage<T>(T message) where T:IComponentMessage
        {
            if (!_messageRegistry.Any(t => t.MessageID == message.MessageID))
                throw new ArgumentException("Message not registered");
            if (SceneObject.Scene == null)
                throw new ArgumentException("Not in any scene");

            // Find connection
            Connection? conn = OwningConnection;
            if (conn == null)
                conn = _server.ServerConnection;

            // Create packet
            ComponentMessagePacket pkt = new ComponentMessagePacket();
            pkt.ObjectID = SceneObject.ID;
            pkt.ScenePath = SceneObject.Scene.Path;
            pkt.MessengerComponentIndex = Array.IndexOf(SceneObject.Components, this);
            pkt.Room = _room;
            if (Game.DebugMode)
            {
                // Add debug information                
                pkt.HasDebugHeaders = true;
                pkt.DebugRemoteComponentTypeName = GetType().FullName;
                pkt.DebugComponentMessageRegistry = new Dictionary<string, int>();

                // Add registry
                int index = 0;
                foreach (IComponentMessage msg in _messageRegistry)
                    pkt.DebugComponentMessageRegistry[msg.MessageID] = index++;
            }
            pkt.MessageID = _messageRegistry.FindIndex(t => t.MessageID == message.MessageID);
            pkt.MessagePayload = SerializingObjects.SerializeObject(message);

            // Send message
            conn.GetChannel<SceneReplicationChannel>().SendPacket(pkt);
        }

        /// <summary>
        /// Called to handle component messages
        /// </summary>
        /// <param name="message">Message instance to handle</param>
        /// <param name="sendMessage">Message sender callback, use this for easy access to reply sending</param>
        protected virtual void HandleMessage(IComponentMessage message, ComponentMessageSender sendMessage) { }

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

        /// <summary>
        /// Retrieves the object scene
        /// </summary>
        public Scene? Scene
        {
            get
            {
                return _object.Scene;
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
        public virtual void Deserialize(Dictionary<string, object?> data) { }

        /// <inheritdoc/>
        public virtual void Serialize(Dictionary<string, object?> data) { }

        /// <summary>
        /// Called when the owning connection disconnects
        /// </summary>
        /// <param name="reason">Disconnect reason</param>
        /// <param name="args">Disconnect arguments</param>
        public virtual void Disconnect(string reason, string[] args) { }
    }
}
