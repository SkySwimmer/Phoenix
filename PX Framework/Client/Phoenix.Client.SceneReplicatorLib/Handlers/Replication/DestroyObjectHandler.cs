using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
{
    public class DestroyObjectHandler : PacketHandler<DestroyObjectPacket>
    {
        protected override PacketHandler<DestroyObjectPacket> CreateInstance()
        {
            return new DestroyObjectHandler();
        }

        protected override bool Handle(DestroyObjectPacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.IsSubscribedToScene(packet.ScenePath) && comp.IsSubscribedToRoom(packet.Room) && comp.Bindings != null)
                {
                    comp.Bindings.RunOnNextFrameUpdate(() =>
                    {
                        IReplicatingSceneObject? obj = comp.Bindings.GetObjectInScene(packet.Room, packet.ScenePath, packet.ObjectID);
                        if (obj != null)
                        {
                            comp.GetLogger().Trace("Destroying object " + packet.ObjectID + " in scene " + packet.ScenePath + " of room " + packet.Room + "..");
                            obj.Destroy();
                        }
                    });
                }
            }
            return true;
        }
    }
}
