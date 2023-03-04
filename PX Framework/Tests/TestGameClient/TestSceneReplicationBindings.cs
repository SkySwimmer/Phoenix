using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Common.Tasks;

namespace TestGameClient
{
    public class TestSceneReplicationBindings : SceneReplicationBindings
    {
        public static TestSceneReplicationBindings inst;
        private SceneReplicationComponent comp;
        public TestSceneReplicationBindings(SceneReplicationComponent component)
        {
            comp = component;
            inst = this;
        }

        public override string GetName()
        {
            return "TestGameClient";
        }

        public override IReplicatingSceneObject GetObjectInScene(string room, string scenePath, string objectPath)
        {
            return new DummySceneObject();
        }

        public override void LoadScene(string scenePath, bool additive)
        {
            comp.BeginLoadingScene(scenePath);
            comp.FinishLoadingScene(scenePath);
        }

        public override void OnBeginInitialSync(string room, string scenePath)
        {
        }

        public override void OnFinishInitialSync(string room, string scenePath)
        {
        }

        public override void RunOnNextFrameUpdate(Action action)
        {
            comp.ServiceManager.GetService<TaskManager>().Oneshot(action);
        }

        public override void SpawnPrefab(SpawnPrefabPacket packet)
        {
        }

        public override void UnloadScene(string scenePath)
        {
        }
    }

    public class DummySceneObject : IReplicatingSceneObject
    {
        public void ChangeScene(string newScene)
        {
        }

        public void Destroy()
        {
        }

        public void Reparent(string newParent)
        {
        }

        public void Replicate(ReplicateObjectPacket packet)
        {
        }
    }
}
