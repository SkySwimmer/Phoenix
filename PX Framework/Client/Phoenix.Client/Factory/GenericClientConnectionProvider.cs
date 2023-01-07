using Phoenix.Client.Components;
using Phoenix.Client.Providers;
using Phoenix.Common.Networking.Connections;

namespace Phoenix.Client
{
    public class GenericClientConnectionProvider : Component, IClientConnectionProvider
    {
        private ClientConstructor cl;
        private Connection? conn;
        private IClientConnectionProvider.ConnectionInfo info;

        public GenericClientConnectionProvider(ClientConstructor cl, IClientConnectionProvider.ConnectionInfo info)
        {
            this.cl = cl;
            this.info = info;
        }

        public override string ID => "client-connection-provider";

        public Connection Provide()
        {
            conn = cl();
            return conn;
        }

        public IClientConnectionProvider.ConnectionInfo ProvideInfo()
        {
            return info;
        }

        public void StartGameClient()
        {
            conn?.Open();
        }

        public void StopGameClient()
        {
            conn?.Close();
        }

        protected override void Define()
        {
        }
    }
}