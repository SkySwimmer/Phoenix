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
        public override void Start()
        {
            base.Start();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Disconnect(string reason, string[] args)
        {
            SceneObject.Destroy();
        }
    }
}
