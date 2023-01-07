using Phoenix.Common.Certificates;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Registry;
using System;
using System.IO;

namespace ChatExampleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ChannelRegistry registry = new ChannelRegistry();
            registry.Register(new TestChannel());
            //Connection conn = Connections.CreateNetworkClient(File.ReadAllText("chat.ip.info"), 6971, registry, PXClientsideCertificate.Download(File.ReadAllText("chat.px.info"), File.ReadAllText("game.id.info"), File.ReadAllText("server.id.info")));
            Connection conn = Connections.CreateNetworkClient("localhost", 16719, registry, PXClientsideCertificate.Download(File.ReadAllText("chat.px.info"), Connections.DownloadServerGameID("localhost", 16719), Connections.DownloadServerID("localhost", 16719)));
            conn.CustomHandshakes += (conn, args) => {
                // Phoenix handshake
                string gid = args.ClientInput.ReadString();
                int prot = args.ClientInput.ReadInt();
                args.ClientOutput.WriteString(gid);
                args.ClientOutput.WriteInt(prot);

                // The details used to connect
                args.ClientOutput.WriteString("localhost");
                args.ClientOutput.WriteInt(16719);
            };
            conn.Open();
            Console.Write("Nickname: ");
            string nick = Console.ReadLine();
            while (true)
            {
                string message = Console.ReadLine();
                conn.GetChannel<TestChannel>().SendPacket(new TestPacket()
                {
                    Sender = nick,
                    Message = message
                });
            }
        }
    }
}
