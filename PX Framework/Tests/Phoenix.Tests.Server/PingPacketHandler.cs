using Phoenix.Common.Networking.Packets;

namespace Phoenix.Tests.Server
{
    internal class PingPacketHandler : PacketHandler<PingPacket>
    {
        protected override PacketHandler<PingPacket> CreateInstance()
        {
            return new PingPacketHandler();
        }

        protected override bool Handle(PingPacket packet)
        {
            GetChannel().SendPacket(new PongPacket());
            return true;
        }
    }
}