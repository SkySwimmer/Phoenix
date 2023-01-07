using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Object Destroy Packet
    /// </summary>
    public class DestroyObjectPacket : AbstractNetworkPacket
    {
        public string ObjectPath = "";
        public string ScenePath = "";
        public string Room = "";

        public override bool Synchronized => true;

        public override AbstractNetworkPacket Instantiate()
        {
            return new DestroyObjectPacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();

            ObjectPath = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);

            writer.WriteString(ObjectPath);
        }
    }
}
