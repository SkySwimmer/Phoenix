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
                        IReplicatingSceneObject? obj = comp.Bindings.GetObjectByIDInScene(packet.Room, packet.ScenePath, packet.ObjectID);
                        if (obj != null && (packet.NewParentID == null || comp.Bindings.GetObjectByIDInScene(packet.Room, packet.ScenePath, packet.NewParentID) != null))
                        {
                            comp.GetLogger().Trace("Reparenting object " + packet.ObjectID + " to " + (packet.NewParentID  == null ? "<root>" : packet.NewParentID) + " in scene " + packet.ScenePath + " in room " + packet.Room + "...");
                            obj.Reparent(packet.NewParentID);
                        }
                    });
                }
            }
            return true;
        }
    }
}
