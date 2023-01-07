using Phoenix.Common.Certificates;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Registry;
using System;
using System.IO;
using System.Threading;

namespace ChatExampleServer
{
    class Program
    {
        public static ServerConnection Server;

        static void Main(string[] args)
        {
            Logger log = Logger.GetLogger("SERVER");
            ChannelRegistry registry = new ChannelRegistry();
            registry.Register(new TestChannel());
            Server = Connections.CreateNetworkServer(6971, registry, PXServerCertificate.FromJson(File.ReadAllText("game.id.info"), File.ReadAllText("certificate.json")));
            Server.Open();
            log.Info("Started example chat on 6971");
            while (Server.IsConnected())
                Thread.Sleep(100);
        }
    }
}
