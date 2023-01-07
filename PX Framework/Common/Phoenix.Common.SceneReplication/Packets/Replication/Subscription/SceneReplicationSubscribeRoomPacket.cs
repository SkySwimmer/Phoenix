using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Scene replication room subscription packet - Called when the server subscribes the client to a room (does not need a response)
    /// </summary>
    public class SceneReplicationSubscribeRoomPacket : AbstractNetworkPacket
    {
        public string Room = "";
        
        public override AbstractNetworkPacket Instantiate()
        {
            return new SceneReplicationSubscribeRoomPacket();
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
