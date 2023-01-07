using Phoenix.Client.Components;
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
                        comp.GetLogger().Trace("Begun initial scene replication for scene " + packet.ScenePath + " in room " + packet.Room);
                        comp.Bindings?.OnBeginInitialSync(packet.Room, packet.ScenePath);
                    });
                }
            }
            return true;
        }
    }
}
