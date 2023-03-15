using Phoenix.Common.SceneReplication.Messages;
using Phoenix.Server.SceneReplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.Tests.Server
{
    public class TestObjectComponent : AbstractObjectComponent
    {
        protected override void RegisterMessages()
        {
            RegisterMessage(new TestMessage());
        }

        public override void Start()
        {
            SendMessage(new TestMessage()
            {
                MessagePayload = "test"
            });
        }

        public override void Update()
        {
            base.Update();
        }

        [MessageHandler]
        public void HandleTest(TestMessage msg, ComponentMessageSender replySender)
        {
            msg = msg;

            replySender(new TestMessage()
            {
                MessagePayload = "Response"
            });
        }

        public override void Disconnect(string reason, string[] args)
        {
            SceneObject.Destroy();
        }
    }
}
