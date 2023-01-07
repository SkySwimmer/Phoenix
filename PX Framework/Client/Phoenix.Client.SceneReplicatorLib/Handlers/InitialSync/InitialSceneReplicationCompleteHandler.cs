using Phoenix.Client.Components;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.InitialSync
{
    public class InitialSceneReplicationCompleteHandler : PacketHandler<InitialSceneReplicationCompletePacket>
    {
        protected override PacketHandler<InitialSceneReplicationCompletePacket> CreateInstance()
        {
            return new InitialSceneReplicationCompleteHandler();
        }

        protected override bool Handle(InitialSceneReplicationCompletePacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.IsSubscribedToScene(packet.ScenePath) && comp.IsSubscribedToRoom(packet.Room) && comp.Bindings != null)
                {
                    comp.Bindings.RunOnNextFrameUpdate(() =>
                    {
                        comp.GetLogger().Trace("Finished initial scene replication for scene " + packet.ScenePath + " in room " + packet.Room);
                        comp.Bindings.OnFinishInitialSync(packet.Room, packet.ScenePath);
                    });
                }
            }
            return true;
        }
    }
}
