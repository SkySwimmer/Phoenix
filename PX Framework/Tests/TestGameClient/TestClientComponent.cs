using Phoenix.Client.SceneReplicatorLib.Messages;
using Phoenix.Common.SceneReplication.Messages;
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

        public void Update()
        {
            // ...

            // Example
            // messenger.SendMessage(new InteractionStartMessage() { ObjectID = messenger.ObjectID });

            // ...
        }

        public void HandleMessage(IComponentMessage message, ComponentMessenger messenger)
        {
            
        }

        public void SetupMessenger(ComponentMessenger messenger)
        {
            this.messenger = messenger;
        }
    }
}
