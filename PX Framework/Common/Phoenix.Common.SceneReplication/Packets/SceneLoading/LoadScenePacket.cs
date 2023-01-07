using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Scene loading packet
    /// </summary>
    public class LoadScenePacket : AbstractNetworkPacket
    {
        public string ScenePath = "";
        public bool Additive = false;

        public override bool Synchronized => true;

        public override AbstractNetworkPacket Instantiate()
        {
            return new LoadScenePacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Additive = reader.ReadBoolean();
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteBoolean(Additive);
        }
    }
}
