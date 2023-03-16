using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Messages;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
{
    public class SpawnPrefabHandler : PacketHandler<SpawnPrefabPacket>
    {
        protected override PacketHandler<SpawnPrefabPacket> CreateInstance()
        {
            return new SpawnPrefabHandler();
        }

        protected override bool Handle(SpawnPrefabPacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.IsSubscribedToScene(packet.ScenePath) && comp.IsSubscribedToRoom(packet.Room) && comp.Bindings != null)
                {
                    comp.Bindings.RunOnNextFrameUpdate(() =>
                    {
                        comp.GetLogger().Trace("Spawning prefab " + packet.PrefabPath + " in scene " + packet.ScenePath + " of room " + packet.Room + ", parent object: " + (packet.ParentObjectID == null ? "<root object>" : packet.ParentObjectID) + "...");
                        comp.Bindings.SpawnPrefab(packet);

                        // Setup
                        comp.GetLogger().Trace("Setting up object " + packet.ObjectID + "...");
                        IComponentMessageReceiver[] components = comp.Bindings.GetNetworkedComponents(packet.Room, packet.ScenePath, packet.ObjectID);
                        int i = 0;
                        foreach (IComponentMessageReceiver component in components)
                        {
                            // Set up component
                            ComponentMessenger messenger = new ComponentMessenger(GetChannel().Connection, component,
                                () => comp.Bindings.GetObjectPathByID(packet.Room, packet.ScenePath, packet.ObjectID),
                                packet.ObjectID, packet.ScenePath, i++, packet.Room);
                            component.SetupMessenger(messenger);

                            // Add messenger instance to memory
                            if (component.Messengers == null)
                                component.Messengers = new Dictionary<string, ComponentMessenger>();
                            component.Messengers[packet.Room] = messenger;
                        }
                    });
                }
            }
            return true;
        }
    }
}
