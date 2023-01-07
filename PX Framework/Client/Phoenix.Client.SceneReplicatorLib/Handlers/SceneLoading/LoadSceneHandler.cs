using Phoenix.Client.Components;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.SceneLoading
{
    public class LoadSceneHandler : PacketHandler<LoadScenePacket>
    {
        protected override PacketHandler<LoadScenePacket> CreateInstance()
        {
            return new LoadSceneHandler();
        }

        protected override bool Handle(LoadScenePacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.Bindings != null)
                {
                    comp.GetLogger().Trace("Loading scene " + packet.ScenePath + "...");
                    comp.Bindings.LoadScene(packet.ScenePath, packet.Additive);
                }
            }
            return true;
        }
    }
}
