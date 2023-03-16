using Phoenix.Common.SceneReplication.Messages;
using Phoenix.Common.Tasks;
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
            Server.ServiceManager.GetService<TaskManager>().AfterSecs(() =>
            {
                SendMessage(new TestMessage()
                {
                    MessagePayload = "test"
                });
            }, 3);
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
