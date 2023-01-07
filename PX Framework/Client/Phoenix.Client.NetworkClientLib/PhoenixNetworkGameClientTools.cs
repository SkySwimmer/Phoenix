using Phoenix.Client.Providers;
using Phoenix.Common.Certificates;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;

namespace Phoenix.Client
{
    public static class PhoenixNetworkGameClientTools
    {
        /// <summary>
        /// Creates a network client
        /// </summary>
        /// <param name="fac">Client factory</param>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server port</param>
        /// <param name="api">Phoenix API for certificate downloads</param>
        public static GameClientFactory WithNetworkClient(this GameClientFactory fac, string ip, int port, string api = "https://aerialworks.ddns.net")
        {
            fac.WithConnectionProvider((InsecureModeCallback InsecureModeCallback, ref IClientConnectionProvider.ConnectionInfo connInfo) =>
            {
                // Attempt connect
                connInfo = new IClientConnectionProvider.ConnectionInfo(ip, port);
                Connections.ServerInfo info = Connections.DownloadServerInfo(ip, port);
                if (!info.SecureMode)
                {
                    if (!InsecureModeCallback())
                        return null;
                    else
                        return () =>
                        {
                            Logger.GetLogger("network-client").Info("Connecting to server at " + ip + " with port " + port + "...");
                            return Connections.CreateNetworkClient(info.Address, info.Port, fac.ChannelRegistry, null);
                        };
                }
                else
                    return () =>
                    {
                        Logger.GetLogger("network-client").Info("Connecting to server at " + ip + " with port " + port + "...");
                        return Connections.CreateNetworkClient(info.Address, info.Port, fac.ChannelRegistry, PXClientsideCertificate.Download(api, info.GameID, info.ServerID));
                    };
            });
            return fac;
        }
    }
}
