using Phoenix.Client.Providers;
using Phoenix.Common.Certificates;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;

namespace Phoenix.Client
{
    public static class PhoenixDebugGameClientTools
    {
        /// <summary>
        /// Creates a debug network client (insecure-mode)
        /// </summary>
        /// <param name="fac">Client factory</param>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server port</param>
        public static GameClientFactory WithDebugClient(this GameClientFactory fac, string ip, int port)
        {
            fac.WithConnectionProvider((InsecureModeCallback InsecureModeCallback, ref IClientConnectionProvider.ConnectionInfo connInfo) =>
            {
                // Attempt connect
                connInfo = new IClientConnectionProvider.ConnectionInfo(ip, port);
                return () =>
                {
                    Logger.GetLogger("network-client").Info("Connecting to server at " + ip + " with port " + port + "...");
                    return Connections.CreateNetworkClient(ip, port, fac.ChannelRegistry, null);
                };
            });
            return fac;
        }
    }
}
