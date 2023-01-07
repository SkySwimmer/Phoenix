using Phoenix.Client.Components;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.SceneLoading
{
    public class UnloadSceneHandler : PacketHandler<UnloadScenePacket>
    {
        protected override PacketHandler<UnloadScenePacket> CreateInstance()
        {
            return new UnloadSceneHandler();
        }

        protected override bool Handle(UnloadScenePacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.Bindings != null)
                {
                    comp.GetLogger().Trace("Unloading scene " + packet.ScenePath + "...");
                    comp.Bindings.UnloadScene(packet.ScenePath);
                }
            }
            return true;
        }
    }
}
