using Phoenix.Common.Networking.Connections;
using Phoenix.Server;
using Phoenix.Server.ServerImplementations;
using System.Net;

namespace Phoenix.Debug.DebugServerLib
{
    public class DebugServerComponent : ServerComponent, IServerProvider
    {
        public override string ID => "debug-server-provider";

        protected override string ConfigurationKey => "server";

        public ServerConnection ProvideServer()
        {
            return Connections.CreateNetworkServer(Server.Address == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(Server.Address), Server.Port, Server.ChannelRegistry, null);
        }

        public override void PreInit()
        {
            if (Server.HasConfigManager)
            {
                GetLogger().Trace("Loading server config...");

                // Port
                GetLogger().Debug("Loading port from configuration...");
                int confPort = Server.GetConfiguration("server").GetInteger("port");
                if (confPort == -1)
                {
                    GetLogger().Debug("Adding port entry to server configuration... Setting port " + Server.Port + "...");
                    Server.GetConfiguration("server").SetInteger("port", Server.Port);
                    confPort = Server.GetConfiguration("server").GetInteger("port");
                    Server.Port = confPort;
                }
                else
                    Server.Port = confPort;
                GetLogger().Trace("Server port overridden from configuration: " + Server.Port);

                // IP
                GetLogger().Debug("Loadign IP from configuration...");
                string? confIP = Server.GetConfiguration("server").GetString("address");
                if (confIP == null)
                {
                    GetLogger().Debug("Adding address entry to server configuration... Setting address " + Server.Address + "...");
                    Server.GetConfiguration("server").SetString("address", Server.Address);
                    confIP = Server.GetConfiguration("server").GetString("address");
                    Server.Address = confIP;
                }
                else
                    Server.Address = confIP;
                GetLogger().Trace("Server IP overridden from configuration: " + Server.Address);
            }
        }

        public void StartGameServer()
        {
            Server.ServerLogger.Info("Starting server on port " + Server.Port + "...");
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
