using Phoenix.Common.IO;
using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.Networking.Internal
{
    public delegate bool PacketHandler(int packetID, DataReader reader);
    public abstract class ConnectionContext
    {
        public abstract void PassPacketHandler(PacketHandler handler);
        public abstract void SendPacket(int id, AbstractNetworkPacket packet, PacketChannel channel);
        public abstract Connection GetConnection();
    }

    public class ConnectionContextImplementer : ConnectionContext
    {
        public delegate Connection ConnectionRetriever();
        public delegate void PassPacketHandlerCall(PacketHandler handler);
        public delegate void SendPacketCall(int id, AbstractNetworkPacket packet, PacketChannel channel);

        public ConnectionRetriever c1;
        public PassPacketHandlerCall c2;
        public SendPacketCall c3;

        public ConnectionContextImplementer(ConnectionRetriever c1, PassPacketHandlerCall c2, SendPacketCall c3)
        {
            this.c1 = c1;
            this.c2 = c2;
            this.c3 = c3;
        }

        public override Connection GetConnection()
        {
            return c1();
        }

        public override void PassPacketHandler(PacketHandler handler)
        {
            c2(handler);
        }

        public override void SendPacket(int id, AbstractNetworkPacket packet, PacketChannel channel)
        {
            c3(id, packet, channel);
        }
    }
}
