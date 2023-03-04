using Phoenix.Common.Networking.Packets;

namespace Phoenix.Tests.Server
{
    public class TestSyncHandler : PacketHandler<TestSyncPacket>
    {
        protected override PacketHandler<TestSyncPacket> CreateInstance()
        {
            return new TestSyncHandler();
        }

        protected override bool Handle(TestSyncPacket packet)
        {
            TestPlayerCharacterContainer? cont = GetChannel().Connection.GetObject<TestPlayerCharacterContainer>();
            if (cont != null)
            {
                // Apply changes
                cont.Character.Transform.Position = new Phoenix.Server.SceneReplication.Coordinates.Vector3(packet.Transform.Position.X, packet.Transform.Position.Y, packet.Transform.Position.Z);
                cont.Character.Transform.Scale = new Phoenix.Server.SceneReplication.Coordinates.Vector3(packet.Transform.Scale.X, packet.Transform.Scale.Y, packet.Transform.Scale.Z);
                cont.Character.Transform.Rotation = new Phoenix.Server.SceneReplication.Coordinates.Vector3(packet.Transform.Rotation.X, packet.Transform.Rotation.Y, packet.Transform.Rotation.Z);
            }
            return true;
        }
    }
}