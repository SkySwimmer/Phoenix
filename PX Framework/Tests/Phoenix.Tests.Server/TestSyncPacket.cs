using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.Tests.Server
{
    public class TestSyncPacket : AbstractNetworkPacket
    {
        public Transform Transform;

        public override AbstractNetworkPacket Instantiate()
        {
            return new TestSyncPacket();
        }

        public override void Parse(DataReader reader)
        {
            Transform = new Transform(new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()), new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()), new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()));
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteFloat(Transform.Position.X);
            writer.WriteFloat(Transform.Position.Y);
            writer.WriteFloat(Transform.Position.Z);
            writer.WriteFloat(Transform.Scale.X);
            writer.WriteFloat(Transform.Scale.Y);
            writer.WriteFloat(Transform.Scale.Z);
            writer.WriteFloat(Transform.Rotation.X);
            writer.WriteFloat(Transform.Rotation.Y);
            writer.WriteFloat(Transform.Rotation.Z);
        }
    }
}
