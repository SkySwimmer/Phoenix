using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Common.Networking.Packets;
using Phoenix.Server.SceneReplication;
using Phoenix.Common;
using Phoenix.Common.Logging;
using Phoenix.Common.SceneReplication.Messages;
using System.Reflection;

namespace Phoenix.Server.Components.SceneReplication.Handlers
{
    public class ComponentMessagePacketHandler : PacketHandler<ComponentMessagePacket>
    {
        protected override PacketHandler<ComponentMessagePacket> CreateInstance()
        {
            return new ComponentMessagePacketHandler();
        }

        protected override bool Handle(ComponentMessagePacket packet)
        {
            // Handle packet

            // Get game server
            GameServer? server = GetChannel().Connection.GetObject<GameServer>();
            if (server != null)
            {
                // Get scene manager
                SceneManager manager = server.ServiceManager.GetService<SceneManager>();

                // Check room and scene
                if (manager.RoomExists(packet.Room) && manager.IsSceneLoaded(packet.ScenePath, packet.Room))
                {
                    // Find object
                    Scene sc = manager.GetScene(packet.ScenePath, packet.Room);
                    SceneObject? obj = sc.Objects.FirstOrDefault(t => t.ID == packet.ObjectID);
                    if (obj != null)
                    {
                        // Find component
                        if (packet.MessengerComponentIndex >= 0 && packet.MessengerComponentIndex < obj.Components.Length)
                        {
                            // Find component
                            AbstractObjectComponent comp = obj.Components[packet.MessengerComponentIndex];
                            comp.SetupNetwork(server, packet.Room);

                            // Handle debug headers
                            if (packet.HasDebugHeaders)
                            {
                                // Check registry length
                                if (comp._messageRegistry.Count != packet.DebugComponentMessageRegistry.Count)
                                {
                                    Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: message registry mismatch, message count does not match, remote end out of sync.\n"
                                        + "\nScene: " + sc.Path
                                        + "\nScene object: " + obj.Path
                                        + "\nComponent: " + comp.GetType().FullName
                                        + "\nRemote component: " + packet.DebugRemoteComponentTypeName
                                        + "\n"
                                        + "\nRemote registry size: " + packet.DebugComponentMessageRegistry.Count
                                        + "\nLocal registry size: " + comp._messageRegistry.Count);
                                    return false;
                                }
                                else
                                {
                                    // Check IDs
                                    if (packet.DebugComponentMessageRegistry.Keys.ToArray()[packet.MessageID] != comp._messageRegistry[packet.MessageID].MessageID)
                                    {
                                        Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: message registry mismatch, message ID does not match, remote end out of sync.\n"
                                        + "\nScene: " + sc.Path
                                        + "\nScene object: " + obj.Path
                                        + "\nComponent: " + comp.GetType().FullName
                                        + "\nRemote component: " + packet.DebugRemoteComponentTypeName
                                        + "\n"
                                        + "\nMessage ID: " + packet.MessageID
                                        + "\nRemote ID string: " + packet.DebugComponentMessageRegistry.Keys.ToArray()[packet.MessageID]
                                        + "\nLocal ID string: " + comp._messageRegistry[packet.MessageID].MessageID
                                        + "\nLocal message type: " + comp._messageRegistry[packet.MessageID].GetType().FullName);
                                        return false;
                                    }
                                }
                            }
                            
                            // Find message handler
                            if (packet.MessageID >= 0 && packet.MessageID < comp._messageRegistry.Count)
                            {
                                // Decode message
                                IComponentMessage msg = comp._messageRegistry[packet.MessageID].CreateInstance();
                                msg.Deserialize(packet.MessagePayload);

                                // Find handler
                                foreach (MethodInfo meth in comp.GetType().GetMethods())
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
                                                meth.Invoke(comp, new object[] {
                                                    msg, comp.InternalReplySender(GetChannel().Connection)
                                                });
                                            }
                                        }
                                    }
                                }

                                // Default handler
                                comp.HandleMessageInternal(msg, GetChannel().Connection);
                            }
                            else if (Game.DebugMode)
                            {
                                Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: message index was out of range. Component: " + comp.GetType().FullName + ", scene object: " + obj.Path + ", scene: " + sc.Path + ". Note: there were no debug headers, unable to show further information, please run both sides in debug mode to run registry checks.");
                                return false;
                            }
                        }
                        else if (Game.DebugMode)
                        {
                            Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: index of component was out of range, please make sure that the coponents are in the same order on both client and server. Scene object: " + obj.Path + ", scene: " + sc.Path + (packet.HasDebugHeaders ? ", remote component type: " + packet.DebugRemoteComponentTypeName : ". Note: there were no debug headers present, unable to show the remote component name."));
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
