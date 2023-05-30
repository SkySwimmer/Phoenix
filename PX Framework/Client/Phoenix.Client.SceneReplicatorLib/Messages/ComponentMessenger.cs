using Phoenix.Client.Components;
using Phoenix.Common;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.SceneReplication;
using Phoenix.Common.SceneReplication.Data;
using Phoenix.Common.SceneReplication.Messages;
using Phoenix.Common.SceneReplication.Packets;
using System.Diagnostics;
using System.Reflection;

namespace Phoenix.Client.SceneReplicatorLib.Messages
{
    /// <summary>
    /// Component message helper - used to send component messages
    /// </summary>
    public class ComponentMessenger
    {
        private Connection _connection;

        private IComponentMessageReceiver _comp;
        private string _objID;
        private string _scenePath;
        private int _compIndex;
        private string _room;

        private List<Func<IComponentMessage, bool>> _responseHandlers = new List<Func<IComponentMessage, bool>>();
        private List<IComponentMessage> _messageRegistry = new List<IComponentMessage>();
        private Func<string> _objPathRetriever;

        /// <summary>
        /// Retrieves the room ID the messenger is attached to
        /// </summary>
        public string Room
        {
            get
            {
                return _room;
            }
        }

        /// <summary>
        /// Retrieves the object ID of the networked object attached to the messenger, note that you usually wont need this as the server side of the component already knows the object by reading the server-side SceneObject field.
        /// </summary>
        public string ObjectID
        {
            get
            {
                return _objID;
            }
        }

        internal ComponentMessenger(Connection connection, IComponentMessageReceiver component, Func<string> objPathRetriever, string objID, string scenePath, int compIndex, string room)
        {
            _connection = connection;
            _comp = component;
            _objID = objID;
            _objPathRetriever = objPathRetriever;
            _scenePath = scenePath;
            _compIndex = compIndex;
            _room = room;
        }

        internal bool HandleMessagePacket(ComponentMessagePacket packet, SceneReplicationComponent comp)
        {
            // Handle debug headers
            if (packet.HasDebugHeaders)
            {
                // Check registry length
                if (_messageRegistry.Count != packet.DebugComponentMessageRegistry.Count)
                {
                    Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: message registry mismatch, message count does not match, remote end out of sync.\n"
                        + "\nScene: " + _scenePath
                        + "\nScene object: " + _objPathRetriever()
                        + "\nComponent: " + _comp.GetType().FullName
                        + "\nRemote component: " + packet.DebugRemoteComponentTypeName
                        + "\n"
                        + "\nRemote registry size: " + packet.DebugComponentMessageRegistry.Count
                        + "\nLocal registry size: " + _messageRegistry.Count);
                    return false;
                }
                else
                {
                    // Check IDs
                    if (packet.DebugComponentMessageRegistry.Keys.ToArray()[packet.MessageID] != _messageRegistry[packet.MessageID].MessageID)
                    {
                        Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: message registry mismatch, message ID does not match, local side out of sync.\n"
                        + "\nScene: " + _scenePath
                        + "\nScene object: " + _objPathRetriever()
                        + "\nComponent: " + _comp.GetType().FullName
                        + "\nRemote component: " + packet.DebugRemoteComponentTypeName
                        + "\n"
                        + "\nMessage ID: " + packet.MessageID
                        + "\nRemote ID string: " + packet.DebugComponentMessageRegistry.Keys.ToArray()[packet.MessageID]
                        + "\nLocal ID string: " + _messageRegistry[packet.MessageID].MessageID
                        + "\nLocal message type: " + _messageRegistry[packet.MessageID].GetType().FullName
                        + "\n"
                        + "\nPlease make sure that the remote end has the same registry order as the local side, else the local message type won't match the remote message type.");
                        return false;
                    }
                }
            }

            // Find message handler
            if (packet.MessageID >= 0 && packet.MessageID < _messageRegistry.Count)
            {
                // Decode message
                IComponentMessage msg = _messageRegistry[packet.MessageID].CreateInstance();
                msg.Deserialize(packet.MessagePayload);

                // Handle on frame update
                comp.Bindings?.RunOnNextFrameUpdate(() =>
                {
                    // Find handler
                    foreach (MethodInfo meth in _comp.GetType().GetMethods())
                    {
                        if (!meth.IsStatic && !meth.IsAbstract)
                        {
                            MessageHandlerAttribute? attr = meth.GetCustomAttribute<MessageHandlerAttribute>();
                            if (attr != null)
                            {
                                // Verify parameters
                                var parameters = meth.GetParameters();
                                if (parameters.Length == 2 && parameters[0].ParameterType.IsAssignableFrom(msg.GetType()) && parameters[1].ParameterType.IsAssignableFrom(typeof(ComponentMessageSender)))
                                {
                                    // Call handler
                                    meth.Invoke(_comp, new object[] {
                                    msg, new ComponentMessageSender(msg => SendMessage(msg))
                                });
                                }
                                else if (parameters.Length == 2 && parameters[0].ParameterType.IsAssignableFrom(msg.GetType()) && parameters[1].ParameterType.IsAssignableFrom(typeof(ComponentMessenger)))
                                {
                                    // Call handler
                                    meth.Invoke(_comp, new object[] {
                                    msg, this
                                });
                                }
                            }
                        }
                    }

                    // Default handler
                    _comp.HandleMessage(msg, this);

                    // Handle single-time handlers
                    Func<IComponentMessage, bool>[] handlers;
                    lock (_responseHandlers)
                        handlers = _responseHandlers.ToArray();
                    foreach (Func<IComponentMessage, bool> handler in handlers)
                    {
                        // Attempt to handle
                        if (handler(msg))
                        {
                            // Remove handler as it succeeded running the response handling code
                            lock (_responseHandlers)
                                _responseHandlers.Remove(handler);
                        }
                    }
                });
            }
            else if (Game.DebugMode)
            {
                Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: message index was out of range. Component: " + _comp.GetType().FullName + ", scene object: " + _objPathRetriever() + ", scene: " + _scenePath + ". Note: there were no debug headers, unable to show further information, please run both sides in debug mode to run registry checks.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Registers component messages
        /// </summary>
        /// <param name="message">Component message to register</param>
        public void RegisterMessage(IComponentMessage message)
        {
            if (_messageRegistry.Any(t => t.MessageID == message.MessageID))
                throw new ArgumentException("Message already registered or another exists with the same ID");
            _messageRegistry.Add(message);
        }

        /// <summary>
        /// Sends messages to clients and attaches a response handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <typeparam name="TResp">Response message type</typeparam>
        /// <param name="message">Message instance</param>
        /// <param name="responseHandler">Response handler (returns true if successful, false otherwise, keeps receiving messages until it returns true, which is when its removed)</param>
        public void SendRequestMessage<T, TResp>(T message, Func<TResp, bool> responseHandler) where T : IComponentMessage where TResp : IComponentMessage
        {
            // Create handler
            Func<IComponentMessage, bool> handler = message =>
            {
                if (Debugger.IsAttached)
                {
                    if (message is TResp)
                        return responseHandler((TResp)message);
                }
                else
                {
                    try
                    {
                        if (message is TResp)
                            return responseHandler((TResp)message);
                    }
                    catch
                    {
                    }
                }
                return false;
            };

            // Add handler
            lock (_responseHandlers)
                _responseHandlers.Add(handler);

            // Send request
            SendMessage(message);
        }

        /// <summary>
        /// Sends messages to clients and waits for responses up to 5 seconds before timing out with an IOException
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <typeparam name="TResp">Response message type</typeparam>
        /// <param name="message">Message instance</param>
        public TResp SendRequestMessage<T, TResp>(T message) where T : IComponentMessage where TResp : IComponentMessage
        {
            return SendRequestMessage<T, TResp>(message, 5);
        }

        /// <summary>
        /// Sends messages to clients and waits for responses up to the specified timeout limit before timing out with an IOException
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <typeparam name="TResp">Response message type</typeparam>
        /// <param name="message">Message instance</param>
        /// <param name="timeout">Timeout limit in seconds, -1 for indefinite wait</param>
        public TResp SendRequestMessage<T, TResp>(T message, int timeout) where T : IComponentMessage where TResp : IComponentMessage
        {
            return SendRequestMessage<T, TResp>(message, timeout, t => true);
        }

        /// <summary>
        /// Sends messages to clients and waits for responses up to the specified timeout limit before timing out with an IOException
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <typeparam name="TResp">Response message type</typeparam>
        /// <param name="message">Message instance</param>
        /// <param name="timeout">Timeout limit in seconds, -1 for indefinite wait</param>
        /// <param name="validityCheck">Function run to check validity of a message</param>
        public TResp SendRequestMessage<T, TResp>(T message, int timeout, Func<TResp, bool> validityCheck) where T : IComponentMessage where TResp : IComponentMessage
        {
            if ((timeout <= 0 && timeout != -1) || ((long)timeout * 1000) >= int.MaxValue)
                throw new ArgumentException("Invalid timeout length");

            // Create handler
            bool handled = false;
            TResp? resp = default(TResp);

            // Create handler
            Func<IComponentMessage, bool> handler = msg =>
            {
                if (Debugger.IsAttached)
                {
                    if (msg is TResp)
                    {
                        if (validityCheck((TResp)msg))
                        {
                            handled = true;
                            resp = (TResp)msg;
                        }
                    }
                }
                else
                {
                    try
                    {
                        if (msg is TResp)
                        {
                            if (validityCheck((TResp)msg))
                            {
                                handled = true;
                                resp = (TResp)msg;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                return false;
            };

            // Add handler
            lock (_responseHandlers)
                _responseHandlers.Add(handler);

            // Send request
            SendMessage(message);

            // Wait until timeout
            int busy = 0;
            while (!handled && _connection.IsConnected() && (timeout == -1 || (busy / 100) < timeout))
            {
                if (timeout != -1)
                    busy++;
                Thread.Sleep(10);
            }
            lock (_responseHandlers)
                _responseHandlers.Remove(handler);
            if (((busy / 100) >= timeout && timeout != -1) || resp == null)
                throw new IOException("Timed out");
            else if (!_connection.IsConnected())
                throw new IOException("Connection lost");
            return resp;
        }

        /// <summary>
        /// Sends messages to clients
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="message">Message instance</param>
        public void SendMessage<T>(T message) where T : IComponentMessage
        {
            if (!_messageRegistry.Any(t => t.MessageID == message.MessageID))
                throw new ArgumentException("Message not registered");

            // Create packet
            ComponentMessagePacket pkt = new ComponentMessagePacket();
            pkt.ObjectID = _objID;
            pkt.ScenePath = _scenePath;
            pkt.MessengerComponentIndex = _compIndex;
            pkt.Room = _room;
            if (Game.DebugMode)
            {
                // Add debug information                
                pkt.HasDebugHeaders = true;
                pkt.DebugRemoteComponentTypeName = _comp.GetType().FullName;
                pkt.DebugComponentMessageRegistry = new Dictionary<string, int>();

                // Add registry
                int index = 0;
                foreach (IComponentMessage msg in _messageRegistry)
                    pkt.DebugComponentMessageRegistry[msg.MessageID] = index++;
            }
            pkt.MessageID = _messageRegistry.FindIndex(t => t.MessageID == message.MessageID);
            pkt.MessagePayload = SerializingObjects.SerializeObject(message);

            // Send message
            _connection.GetChannel<SceneReplicationChannel>().SendPacket(pkt);
        }

    }
}
