using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common;
using Phoenix.Common.Logging;
using Phoenix.Common.SceneReplication.Messages;
using System.Reflection;
using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Messages;
using Phoenix.Client.SceneReplicatorLib.Binding;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
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

            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.IsSubscribedToScene(packet.ScenePath) && comp.IsSubscribedToRoom(packet.Room) && comp.Bindings != null)
                {
                    // FIXME: make sure its synced to the engine
                    
                    // Find object
                    IReplicatingSceneObject? obj = comp.Bindings.GetObjectByIDInScene(packet.Room, packet.ScenePath, packet.ObjectID);
                    if (obj != null)
                    {
                        // Find component
                        IComponentMessageReceiver[] components = comp.Bindings.GetNetworkedComponents(packet.Room, packet.ScenePath, packet.ObjectID);
                        if (packet.MessengerComponentIndex >= 0 && packet.MessengerComponentIndex < components.Length)
                        {
                            IComponentMessageReceiver component = components[packet.MessengerComponentIndex];
                            if (component.Messengers != null) {
                                // Retrieve messenger instance
                                ComponentMessenger? messenger = null;
                                lock (component.Messengers)
                                    if (component.Messengers.ContainsKey(packet.Room))
                                        messenger = component.Messengers[packet.Room];
                                if (messenger == null)
                                {
                                    if (Game.DebugMode)
                                    {
                                        Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: component messenger instance was lost. Scene object: " + comp.Bindings.GetObjectPathByID(packet.Room, packet.ScenePath, packet.ObjectID) + ", scene: " + packet.ScenePath + (packet.HasDebugHeaders ? ", remote component type: " + packet.DebugRemoteComponentTypeName : ". Note: there were no debug headers present, unable to show the remote component name."));
                                        return false;
                                    }
                                    return true;
                                }
                                return messenger.HandleMessagePacket(packet);
                            }
                            else
                            {
                                if (Game.DebugMode)
                                {
                                    Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: component messenger instance was lost. Scene object: " + comp.Bindings.GetObjectPathByID(packet.Room, packet.ScenePath, packet.ObjectID) + ", scene: " + packet.ScenePath + (packet.HasDebugHeaders ? ", remote component type: " + packet.DebugRemoteComponentTypeName : ". Note: there were no debug headers present, unable to show the remote component name."));
                                    return false;
                                }
                                return true;
                            }
                        }
                        else if (Game.DebugMode)
                        {
                            Logger.GetLogger("scene-replication").Error("Failed to handle component message packet: index of component was out of range, please make sure that the coponents are in the same order on both client and server. Scene object: " + comp.Bindings.GetObjectPathByID(packet.Room, packet.ScenePath, packet.ObjectID) + ", scene: " + packet.ScenePath + (packet.HasDebugHeaders ? ", remote component type: " + packet.DebugRemoteComponentTypeName : ". Note: there were no debug headers present, unable to show the remote component name."));
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
