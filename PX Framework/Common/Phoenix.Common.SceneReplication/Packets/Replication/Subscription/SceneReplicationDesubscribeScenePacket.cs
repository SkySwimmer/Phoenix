using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Scene Replication Desubscribe Scene Packet - Called when a scene is desubscribed
    /// </summary>
    public class SceneReplicationDesubscribeScenePacket : AbstractNetworkPacket
    {
        public string ScenePath = "";

        public override AbstractNetworkPacket Instantiate()
        {
            return new SceneReplicationDesubscribeScenePacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
        }
    }
}
