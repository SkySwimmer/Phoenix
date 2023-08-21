namespace Phoenix.Client.ServerList
{
    internal class JsonServerListEntry
    {
        public string? id;
        public string? ownerId;
        public string? version;
        public JsonServerListEntryProtocol? protocol;
        public string[] addresses = new string[0];
        public int port;
        public Dictionary<string, string> details = new Dictionary<string, string>();
    }

    internal class JsonServerListEntryProtocol
    {
        public int programVersion;
        public int phoenixVersion;
    }
}
