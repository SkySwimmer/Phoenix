using Phoenix.Client.Providers;
using Phoenix.Client.ServerList;
using Phoenix.Common.Certificates;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;

namespace Phoenix.Client
{
    public static class PhoenixServerListGameClientTools
    {
        /// <summary>
        /// Creates a network client from a server list entry
        /// </summary>
        /// <param name="fac">Client factory</param>
        /// <param name="server">Server list entry</param>
        /// <param name="api">Phoenix API for certificate downloads</param>
        public static GameClientFactory WithNetworkClient(this GameClientFactory fac, ServerInstance server, string api = "https://aerialworks.ddns.net")
        {
            fac.WithConnectionProvider((InsecureModeCallback InsecureModeCallback, ref IClientConnectionProvider.ConnectionInfo connInfo) =>
            {
                // Attempt connect
                connInfo = new IClientConnectionProvider.ConnectionInfo(server.BestAddress, server.ServerPort);
                Connections.ServerInfo info = Connections.DownloadServerInfo(server.BestAddress, server.ServerPort);
                if (!info.SecureMode)
                {
                    if (!InsecureModeCallback())
                        return null;
                    else
                        return () =>
                        {
                            Logger.GetLogger("network-client").Info("Connecting to server at " + server.BestAddress + " with port " + server.ServerPort + "...");
                            return Connections.CreateNetworkClient(server.IsLanServer ? "lesssecure:" + server.BestAddress : server.BestAddress, server.ServerPort, fac.ChannelRegistry, null);
                        };
                }
                else
                    return () =>
                    {
                        Logger.GetLogger("network-client").Info("Connecting to server at " + server.BestAddress + " with port " + server.ServerPort + "...");
                        return Connections.CreateNetworkClient(server.IsLanServer ? "lesssecure:" + server.BestAddress : server.BestAddress, info.Port, fac.ChannelRegistry, PXClientsideCertificate.Download(api, info.GameID, info.ServerID));
                    };
            });
            return fac;
        }
    }
}
