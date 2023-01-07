using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Scene replication scene subscription packet - Called when the server subscribes the client to a room and awaits a response.<br/>
    /// The server requires it to be sent back with success on true or false when the scene is ready or should it fail to load
    /// </summary>
    public class SceneReplicationSubscribeScenePacket : AbstractNetworkPacket
    {
        public string ScenePath = "";
        public string Room = "";
        public bool Success = false;

        public override AbstractNetworkPacket Instantiate()
        {
            return new SceneReplicationSubscribeScenePacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();
            Success = reader.ReadBoolean();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);
            writer.WriteBoolean(Success);
        }
    }
}
