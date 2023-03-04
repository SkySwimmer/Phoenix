using Phoenix.Common.Events;
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
        }

        [EventListener]
        public void ClientConnected(ClientConnectedEvent ev)
        {
            ev.Client.AddObject(new SceneReplicator(ev.Client, ServiceManager.GetService<SceneManager>()));
        }
    }
}