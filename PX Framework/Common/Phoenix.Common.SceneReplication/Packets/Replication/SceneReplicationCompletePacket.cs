using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Scene replication completion packet - Called when replication finishes
    /// </summary>
    public class SceneReplicationCompletePacket : AbstractNetworkPacket
    {
        public string ScenePath = "";
        public string Room = "";

        public override bool Synchronized => true;

        public override AbstractNetworkPacket Instantiate()
        {
            return new SceneReplicationCompletePacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);
        }
    }
}
