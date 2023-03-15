using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Messages;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.InitialSync
{
    public class InitialSceneReplicationStartHandler : PacketHandler<InitialSceneReplicationStartPacket>
    {
        protected override PacketHandler<InitialSceneReplicationStartPacket> CreateInstance()
        {
            return new InitialSceneReplicationStartHandler();
        }

        protected override bool Handle(InitialSceneReplicationStartPacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.IsSubscribedToScene(packet.ScenePath) && comp.IsSubscribedToRoom(packet.Room) && comp.Bindings != null)
                {
                    comp.Bindings.RunOnNextFrameUpdate(() =>
                    {
                        // Sync
                        comp.GetLogger().Trace("Begun initial scene replication for scene " + packet.ScenePath + " in room " + packet.Room);
                        comp.Bindings?.OnBeginInitialSync(packet.Room, packet.ScenePath, packet.ObjectMap);

                        // Set up components
                        if (comp.Bindings != null)
                        {
                            foreach (string obj in packet.ObjectMap.Keys)
                            {
                                comp.GetLogger().Trace("Setting up object " + obj + "...");
                                IComponentMessageReceiver[] components = comp.Bindings.GetNetworkedComponents(packet.Room, packet.ScenePath, obj);
                                int i = 0;
                                foreach (IComponentMessageReceiver component in components)
                                {
                                    // Set up component
                                    ComponentMessenger messenger = new ComponentMessenger(GetChannel().Connection, component,
                                        () => comp.Bindings.GetObjectPathByID(packet.Room, packet.ScenePath, obj), obj, packet.ScenePath, i++, packet.Room);
                                    component.SetupMessenger(messenger);

                                    // TODO: handling setup
                                }
                            }
                        }
                    });
                }
            }
            return true;
        }
    }
}
