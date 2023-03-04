using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Packets;
using Phoenix.Server;
using Phoenix.Server.Players;

namespace Phoenix.Tests.Server
{
    // Example channel
    public class TestChannel : PacketChannel
    {
        public override PacketChannel Instantiate()
        {
            return new TestChannel();
        }

        protected override void MakeRegistry()
        {
            RegisterPacket(new TestPacket());
            RegisterHandler(new TestPacketHandler());
            RegisterPacket(new TestSyncPacket());

            RegisterPacket(new PingPacket());
            RegisterPacket(new PongPacket());
            RegisterHandler(new PingPacketHandler());
        }
    }

    public class PongPacket : AbstractNetworkPacket
    {
        public override AbstractNetworkPacket Instantiate()
        {
            return new PongPacket();
        }

        public override void Parse(DataReader reader)
        {
        }

        public override void Write(DataWriter writer)
        {
        }
    }

    public class PingPacket : AbstractNetworkPacket
    {
        public override AbstractNetworkPacket Instantiate()
        {
            return new PingPacket();
        }

        public override void Parse(DataReader reader)
        {
        }

        public override void Write(DataWriter writer)
        {
        }
    }

    // Example packet, lets make a very very simple chat
    public class TestPacket : AbstractNetworkPacket
    {
        public string Message = "";
        public string Sender = "";

        public override AbstractNetworkPacket Instantiate()
        {
            return new TestPacket();
        }

        public override void Parse(DataReader reader)
        {
            // Read packet
            Sender = reader.ReadString();
            Message = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            // Write packet
            writer.WriteString(Sender);
            writer.WriteString(Message);
        }
    }

    // Example packet handler
    public class TestPacketHandler : PacketHandler<TestPacket>
    {
        protected override PacketHandler<TestPacket> CreateInstance()
        {
            return new TestPacketHandler();
        }

        protected override bool Handle(TestPacket packet)
        {
            // Lets show the message in the output
            if (packet.Sender == "")
            {
                Player? plr = GetChannel().Connection.GetObject<Player>();
                if (plr != null)
                {
                    packet.Sender = plr.DisplayName;
                }
            }
            Logger.GetLogger("CHAT").Info("Chat: " + packet.Sender + ": " + packet.Message);

            // Broadcast
            GameServer? server = GetChannel().Connection.GetObject<GameServer>();
            if (server != null)
                server.ServerConnection.GetChannel(GetChannel()).SendPacket(packet);
            return true;
        }
    }

}
