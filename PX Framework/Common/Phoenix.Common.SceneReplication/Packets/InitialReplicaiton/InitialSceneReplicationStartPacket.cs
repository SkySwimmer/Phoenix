using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Initial scene replication start packet - Called after a room is subscribed to
    /// </summary>
    public class InitialSceneReplicationStartPacket : AbstractNetworkPacket
    {
        public string ScenePath = "";
        public string Room = "";

        public class SceneObjectID
        {
            public string Path;
            public int Index;

            public SceneObjectID(string path, int index)
            {
                Path = path;
                Index = index;
            }
        }

        public override bool Synchronized => true;

        public Dictionary<string, SceneObjectID> ObjectMap = new Dictionary<string, SceneObjectID>();

        public override AbstractNetworkPacket Instantiate()
        {
            return new InitialSceneReplicationStartPacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();

            int l = reader.ReadInt();
            for (int i = 0; i < l; i++)
                ObjectMap[reader.ReadString()] = new SceneObjectID(reader.ReadString(), reader.ReadInt());
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);

            writer.WriteInt(ObjectMap.Count);
            foreach (string id in ObjectMap.Keys)
            {
                writer.WriteString(id);
                writer.WriteString(ObjectMap[id].Path);
                writer.WriteInt(ObjectMap[id].Index);
            }
        }
    }
}
