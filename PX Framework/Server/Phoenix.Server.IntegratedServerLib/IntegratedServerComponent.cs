using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Impl;
using Phoenix.Server;
using Phoenix.Server.ServerImplementations;
using System.Net;

namespace Phoenix.Server.IntegratedServerLib
{
    /// <summary>
    /// Integrated server component
    /// </summary>
    public class IntegratedServerComponent : ServerComponent, IServerProvider
    {
        public override string ID => "integrated-server-provider";

        protected override string ConfigurationKey => "server";

        private IntegratedServerConnection srvConn;

        /// <summary>
        /// Creates the integrated server component
        /// </summary>
        /// <param name="integratedServer">Integrated server connection</param>
        public IntegratedServerComponent(IntegratedServerConnection integratedServer)
        {
            srvConn = integratedServer;
        }

        public ServerConnection ProvideServer()
        {
            return srvConn;
        }

        public void StartGameServer()
        {
            Server.ServerConnection.Open();
        }

        public void StopGameServer()
        {
            Server.ServerConnection.Close();
        }

        protected override void Define()
        {
        }
    }
}
