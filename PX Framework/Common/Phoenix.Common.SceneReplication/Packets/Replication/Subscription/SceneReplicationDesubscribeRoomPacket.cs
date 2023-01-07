using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Scene Replication Desubscribe Room Packet - Called when a room is desubscribed
    /// </summary>
    public class SceneReplicationDesubscribeRoomPacket : AbstractNetworkPacket
    {
        public string Room = "";

        public override AbstractNetworkPacket Instantiate()
        {
            return new SceneReplicationDesubscribeRoomPacket();
        }

        public override void Parse(DataReader reader)
        {
            Room = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(Room);
        }
    }
}
