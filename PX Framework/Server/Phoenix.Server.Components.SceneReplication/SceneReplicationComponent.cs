using Phoenix.Common.Events;
using Phoenix.Common.SceneReplication;
using Phoenix.Server.Components.SceneReplication.Handlers;
using Phoenix.Server.Events;
using Phoenix.Server.SceneReplication;

namespace Phoenix.Server.Components
{
    public class SceneReplicationComponent : ServerComponent
    {
        public override string ID => "scene-replication";

        protected override string ConfigurationKey => "server";

        protected override void Define()
        {
            LoadBefore("player-manager");
        }

        public override void PreInit()
        {
            ServiceManager.RegisterService(new SceneManager(Server));

            // Get channel
            SceneReplicationChannel channel;
            try
            {
                channel = Server.ChannelRegistry.GetChannel<SceneReplicationChannel>();
            }
            catch
            {
                throw new ArgumentException("No replication packet channel in packet registry. Please add Phoenix.Common.SceneReplication.SceneReplicationChannel to the server packet registry.");
            }

            // Add handler for component messages
            channel.RegisterHandler(new ComponentMessagePacketHandler());
        }

        [EventListener]
        public void ClientConnected(ClientConnectedEvent ev)
        {
            ev.Client.AddObject(new SceneReplicator(ev.Client, ServiceManager.GetService<SceneManager>()));
        }
    }
}