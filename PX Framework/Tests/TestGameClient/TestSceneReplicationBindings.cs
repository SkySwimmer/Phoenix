using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Client.SceneReplicatorLib.Messages;
using Phoenix.Common.SceneReplication.Packets;
using Phoenix.Common.Tasks;

namespace TestGameClient
{
    public class TestSceneReplicationBindings : SceneReplicationBindings
    {
        public static TestSceneReplicationBindings inst;
        internal Dictionary<string, DummySceneObject> objects = new Dictionary<string, DummySceneObject>();
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

        public override IComponentMessageReceiver[] GetNetworkedComponents(string room, string scenePath, string objectID)
        {
            return objects[objectID].Components;
        }

        public override IReplicatingSceneObject GetObjectByIDInScene(string room, string scenePath, string objectID)
        {
            return objects.ContainsKey(objectID) ? objects[objectID] : null;
        }

        public override string GetObjectPathByID(string room, string scenePath, string objectID)
        {
            return objects[objectID].Path;
        }

        public override void LoadScene(string scenePath, bool additive)
        {
            comp.BeginLoadingScene(scenePath);
            comp.FinishLoadingScene(scenePath);
        }

        public override void OnBeginInitialSync(string room, string scenePath, Dictionary<string, InitialSceneReplicationStartPacket.SceneObjectID> objectMap)
        {
            foreach (string id in objectMap.Keys)
            {
                DummySceneObject obj = new DummySceneObject(this);
                obj.ID = id;
                obj.Path = objectMap[id].Path;
                objects[obj.ID] = obj;
            }
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
            DummySceneObject obj = new DummySceneObject(this);
            obj.ID = packet.ObjectID;
            obj.Path = packet.ParentObjectID == null ? Path.GetFileNameWithoutExtension(packet.PrefabPath) : objects[packet.ParentObjectID].Path + "/" + Path.GetFileNameWithoutExtension(packet.PrefabPath);
            objects[obj.ID] = obj;
        }

        public override void UnloadScene(string scenePath)
        {
        }
    }

    public class DummySceneObject : IReplicatingSceneObject
    {
        private TestSceneReplicationBindings bindings;
        public DummySceneObject(TestSceneReplicationBindings bindings)
        {
            this.bindings = bindings;
        }

        public string ID;
        public string Path;

        public IComponentMessageReceiver[] Components = new IComponentMessageReceiver[] { new TestClientComponent() };

        public void ChangeScene(string newScene)
        {
        }

        public void Destroy()
        {
            bindings.objects.Remove(ID);
        }

        public void Reparent(string newParent)
        {
            Path = (newParent == System.IO.Path.GetFileName(newParent) ? "" : System.IO.Path.GetDirectoryName(newParent) + "/") + System.IO.Path.GetFileName(Path);
        }

        public void Replicate(ReplicateObjectPacket packet)
        {
        }
    }
}
