using Phoenix.Client.Providers;
using Phoenix.Common.Certificates;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Impl;

namespace Phoenix.Client
{
    public static class PhoenixIntegratedGameClientTools
    {
        /// <summary>
        /// Simple method to pass a integrated server connection
        /// </summary>
        /// <param name="integratedServerConnection">Integrated server connection</param>
        public delegate void PassIntegratedServer(IntegratedServerConnection integratedServerConnection);

        /// <summary>
        /// Creates a integrated client
        /// </summary>
        /// <param name="fac">Client factory</param>
        /// <param name="integratedServerOutput">Integrated server output</param>
        public static GameClientFactory WithIntegratedClient(this GameClientFactory fac, PassIntegratedServer integratedServerOutput)
        {
            fac.WithConnectionProvider((InsecureModeCallback InsecureModeCallback, ref IClientConnectionProvider.ConnectionInfo connInfo) =>
            {
                return () =>
                {
                    // Create bundle
                    ConnectionBundle bundle = Connections.CreateIntegratedConnections(fac.ChannelRegistry);
                    integratedServerOutput((IntegratedServerConnection)bundle.Server);
                    return bundle.Client;
                };
            });
            return fac;
        }

        /// <summary>
        /// Adds a integrated client
        /// </summary>
        /// <param name="fac">Client factory</param>
        /// <param name="clientConnectionConstr">Client connection constructor</param>
        public static GameClientFactory WithIntegratedClient(this GameClientFactory fac, Func<IntegratedClientConnection> clientConnectionConstr)
        {
            fac.WithConnectionProvider((InsecureModeCallback InsecureModeCallback, ref IClientConnectionProvider.ConnectionInfo connInfo) =>
            {
                return () => clientConnectionConstr();
            });
            return fac;
        }
    }
}
