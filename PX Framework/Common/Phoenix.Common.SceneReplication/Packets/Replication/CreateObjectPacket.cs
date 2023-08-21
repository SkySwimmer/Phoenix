using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Create Object Packet - Called when a empty object is spawned
    /// </summary>
    public class CreateObjectPacket : AbstractNetworkPacket
    {
        public string ObjectID = "";
        public string? ParentObjectID = null;
        public string ScenePath = "";
        public string Room = "";
        
        public override bool Synchronized => true;

        public override AbstractNetworkPacket Instantiate()
        {
            return new SpawnPrefabPacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();

            ObjectID = reader.ReadString();
            if (reader.ReadBoolean())
                ParentObjectID = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);

            writer.WriteString(ObjectID);
            writer.WriteBoolean(ParentObjectID != null);
            if (ParentObjectID != null)
                writer.WriteString(ParentObjectID);
        }
    }
}
