using Phoenix.Client.SceneReplicatorLib.Messages;
using Phoenix.Common.SceneReplication.Messages;
using Phoenix.Tests.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGameClient
{
    public class TestClientComponent : IComponentMessageReceiver
    {
        private ComponentMessenger messenger;

        public Dictionary<string, ComponentMessenger> Messengers { get; set; }

        public void Update()
        {
            // ...

            // Example
            // messenger.SendMessage(new InteractionStartMessage() { ObjectID = messenger.ObjectID });

            // ...
        }

        [MessageHandler]
        public void HandleTest(TestMessage message, ComponentMessenger messenger)
        {
            message = message;
        }

        public void HandleMessage(IComponentMessage message, ComponentMessenger messenger)
        {            
        }

        public void SetupMessenger(ComponentMessenger messenger)
        {
            this.messenger = messenger;

            // Register messages
            messenger.RegisterMessage(new TestMessage());
        }
    }
}
