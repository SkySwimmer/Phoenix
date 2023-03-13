using Phoenix.Common.SceneReplication.Messages;
using System.Collections.Generic;

namespace Phoenix.Tests.Server
{
    public class TestMessage : IComponentMessage
    {
        public string MessageID => "test";
        public string MessagePayload = "";

        public IComponentMessage CreateInstance()
        {
            return new TestMessage();
        }

        public void Deserialize(Dictionary<string, object?> data)
        {
            MessagePayload = data["test"].ToString();            
        }

        public void Serialize(Dictionary<string, object?> data)
        {
            data["test"] = MessagePayload;
        }
    }
}
