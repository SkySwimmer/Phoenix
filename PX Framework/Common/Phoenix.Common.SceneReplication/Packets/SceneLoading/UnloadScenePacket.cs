using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Scene unloading packet
    /// </summary>
    public class UnloadScenePacket : AbstractNetworkPacket
    {
        public string ScenePath = "";

        public override bool Synchronized => true;

        public override AbstractNetworkPacket Instantiate()
        {
            return new UnloadScenePacket();
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
