using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
{
    public class ReparentObjectHandler : PacketHandler<ReparentObjectPacket>
    {
        protected override PacketHandler<ReparentObjectPacket> CreateInstance()
        {
            return new ReparentObjectHandler();
        }

        protected override bool Handle(ReparentObjectPacket packet)
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
                        if (obj != null && (packet.NewParentPath == null || comp.Bindings.GetObjectInScene(packet.Room, packet.ScenePath, packet.NewParentPath) != null))
                        {
                            comp.GetLogger().Trace("Reparenting object " + packet.ObjectID + " to " + (packet.NewParentPath  == null ? "<root>" : packet.NewParentPath) + " in scene " + packet.ScenePath + " in room " + packet.Room + "...");
                            obj.Reparent(packet.NewParentPath);
                        }
                    });
                }
            }
            return true;
        }
    }
}
