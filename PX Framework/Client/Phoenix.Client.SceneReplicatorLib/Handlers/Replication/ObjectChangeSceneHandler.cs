using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
{
    public class ObjectChangeSceneHandler : PacketHandler<ObjectChangeScenePacket>
    {
        protected override PacketHandler<ObjectChangeScenePacket> CreateInstance()
        {
            return new ObjectChangeSceneHandler();
        }

        protected override bool Handle(ObjectChangeScenePacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.IsSubscribedToScene(packet.ScenePath) && comp.IsSubscribedToRoom(packet.Room) && comp.IsSubscribedToScene(packet.NewScenePath) && comp.Bindings != null)
                {
                    comp.Bindings.RunOnNextFrameUpdate(() =>
                    {
                        IReplicatingSceneObject? obj = comp.Bindings.GetObjectInScene(packet.Room, packet.ScenePath, packet.ObjectID);
                        if (obj != null)
                        {
                            comp.GetLogger().Trace("Moving object " + packet.ObjectID + " to scene " + packet.NewScenePath + " in room " + packet.Room + "...");
                            obj.ChangeScene(packet.NewScenePath);
                        }
                    });
                }
            }
            return true;
        }
    }
}
