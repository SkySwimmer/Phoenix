using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Reparent Object Packet
    /// </summary>
    public class ReparentObjectPacket : AbstractNetworkPacket
    {
        public string ObjectID = "";
        public string? NewParentID = null;
        public string ScenePath = "";
        public string Room = "";

        public override bool Synchronized => true;

        public override AbstractNetworkPacket Instantiate()
        {
            return new ReparentObjectPacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();

            ObjectID = reader.ReadString();
            if (reader.ReadBoolean())
                NewParentID = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);

            writer.WriteString(ObjectID);
            writer.WriteBoolean(NewParentID != null);
            if (NewParentID != null)
                writer.WriteString(NewParentID);
        }
    }
}
