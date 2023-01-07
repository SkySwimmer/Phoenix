using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Reparent Object Packet
    /// </summary>
    public class ReparentObjectPacket : AbstractNetworkPacket
    {
        public string ObjectPath = "";
        public string? OldParentPath = null;
        public string? NewParentPath = null;
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

            ObjectPath = reader.ReadString();
            if (reader.ReadBoolean())
                OldParentPath = reader.ReadString();
            if (reader.ReadBoolean())
                NewParentPath = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);

            writer.WriteString(ObjectPath);
            writer.WriteBoolean(OldParentPath != null);
            if (OldParentPath != null)
                writer.WriteString(OldParentPath);
            writer.WriteBoolean(NewParentPath != null);
            if (NewParentPath != null)
                writer.WriteString(NewParentPath);
        }
    }
}
