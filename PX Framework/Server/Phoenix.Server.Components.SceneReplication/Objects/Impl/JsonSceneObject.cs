using Newtonsoft.Json;
using Phoenix.Common.Logging;
using Phoenix.Server.SceneReplication.Coordinates;
using Phoenix.Server.SceneReplication.Data;
using System.Reflection;

namespace Phoenix.Server.SceneReplication.Impl
{
    internal class JsonVector3
    {
        public float x;
        public float y;
        public float z;
    }

    internal class JsonTransform
    {
        public JsonVector3 position;
        public JsonVector3 angles;
        public JsonVector3 scale;
    }

    internal class JsonSceneObjectData
    {
        public string name;
        public bool replicating;
        public bool active;

        public JsonTransform transform;
        public Dictionary<string, object?> replication = new Dictionary<string, object?>();
        public List<JsonComponentInfo> components = new List<JsonComponentInfo>();

        public List<JsonSceneObjectData> children = new List<JsonSceneObjectData>();
    }

    internal class JsonComponentInfo
    {
        public string type;
        public Dictionary<string, object> data;
    }

    internal class JsonSceneObject : SceneObject
    {
        private List<SceneObject> _children = new List<SceneObject>();

        private SceneObject? _parent;
        private Scene? _scene;

        private string _id;
        private string _path;
        private string _name;

        private bool _active;
        private bool _replicating;

        private Transform _transform;
        private ReplicationDataMap _data;

        public JsonSceneObject(JsonSceneObjectData obj)
        {
            // Load fields
            _name = obj.name;
            _path = obj.name;
            _active = obj.active;
            _replicating = obj.replicating;

            // Load transform
            _transform = new(new Vector3(obj.transform.position.x, obj.transform.position.y, obj.transform.position.z, !_replicating), new Vector3(obj.transform.scale.x, obj.transform.scale.y, obj.transform.scale.z, !_replicating), new Vector3(obj.transform.angles.x, obj.transform.angles.y, obj.transform.angles.z, !_replicating));
            _transform.OnReplicate += prop =>
            {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                CallOnReplicate(this, ReplicatingProperty.TRANSFORM, null, Transform);
            };

            // Load data
            _data = new ReplicationDataMap(obj.replication, !_replicating);
            _data.OnChange += (key, val) => {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                CallOnReplicate(this, ReplicatingProperty.REPLICATION_DATA, key, val);
            };
            _data.OnRemove += (key) => {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                CallOnReplicate(this, ReplicatingProperty.REPLICATION_DATA_REMOVEKEY, null, key);
            };

            // Register and set ID
            _id = Register(this);

            // Add components
            foreach (JsonComponentInfo comp in obj.components)
            {
                // Find type
                try
                {
                    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            Type? compType = asm.GetType(comp.type);
                            if (compType != null)
                            {
                                // Found it
                                try
                                {
                                    ConstructorInfo? constr = compType.GetConstructor(new Type[0]);
                                    if (constr == null)
                                        throw new ArgumentException("No parameterless constructor");
                                    object compInst = constr.Invoke(new object[0]);
                                    if (compInst is AbstractObjectComponent)
                                    {
                                        AddComponent((AbstractObjectComponent)compInst).Deserialize(comp.data);
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Instantiated object is not an object component");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.GetLogger("scene-manager").Error("Failed to add component " + comp.type + " to object " + _id + " (" + _path + ")", e);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private void AddChildren(JsonSceneObjectData obj)
        {
            foreach (JsonSceneObjectData data in obj.children)
            {
                JsonSceneObject ch = new JsonSceneObject(data)
                {
                    _parent = this,
                    _scene = this._scene
                };
                ch._path = _path + "/" + ch.Path;
                ch.AddChildren(data);
                lock(_children)
                    _children.Add(ch);
            } 
        }

        public override string ID
        {
            get
            {
                return _id;
            }
        }

        public override string Path
        {
            get
            {
                return _path;
            }
        }

        public override bool Active
        {
            get
            {
                return _active;
            }

            set
            {
                if (!Replicating)
                    throw new ArgumentException("Cannot modify read-only objects");
                bool last = _active;
                _active = value;
                OnChangeActiveState(last);
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (!Replicating)
                    throw new ArgumentException("Cannot modify read-only objects");
                _name = value;
                CallOnReplicate(this, ReplicatingProperty.NAME, null, _name);
            }
        }

        public override bool Replicating
        {
            get
            {
                return _replicating;
            }
        }

        public override Scene? Scene
        {
            get
            {
                return _scene;
            }

            set
            {
                if (!Replicating)
                    throw new ArgumentException("Cannot modify read-only objects");
                if (value == null)
                    throw new ArgumentException("Null scene assignment unsupported as it breaks replication");
                if (_parent != null)
                    Parent = null;
                else if (_scene != null)
                    _scene.RemoveFromScene(this); // Only root nodes need to call this
                Scene? oldScene = _scene;
                Scene newScene = value;
                _scene = value;
                _scene.AddToScene(this);
                CallOnChangeScene(this, oldScene, newScene);
            }
        }

        public override SceneObject? Parent
        {
            get
            {
                return _parent;
            }

            set
            {
                if (!Replicating)
                    throw new ArgumentException("Cannot modify read-only objects");
                SceneObject? oldParent = _parent;
                SceneObject? newParent = value;
                if (oldParent != null)
                    oldParent.RemoveChild(this);
                _path = _name;
                if (newParent != null)
                {
                    _path = newParent.Path + "/" + _path;
                    newParent.AddChild(this);
                    if (_scene != null && _scene.Objects.Any(t => t.ID == ID))
                        _scene.RemoveFromScene(this);
                    if (_scene != newParent.Scene)
                    {
                        Scene? oldScene = _scene;
                        _scene = newParent.Scene;
                        CallOnChangeScene(this, oldScene, _scene);
                    }
                }
                else if (_scene != null && !_scene.Objects.Any(t => t.ID == ID))
                    _scene.AddToScene(this);
                _parent = value;
                CallOnReparent(this, oldParent, newParent);
            }
        }

        public override Transform Transform
        {
            get
            {
                return _transform;
            }
        }

        public override ReplicationDataMap ReplicationData
        {
            get
            {
                return _data;
            }
        }

        public override SceneObject[] Children
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _children.ToArray();
                    }
                    catch { }
                }
            }
        }

        protected override void DestroyForced()
        {
            // Unregister
            Unregister(this);

            // Destroy
            CallOnDestroy(this);

            // Unset
            Scene? oldScene = _scene;
            SceneObject? oldParent = _parent;
            _scene = null;
            _parent = null;

            // Post
            PostCallDestroy(this, oldParent, oldScene);

            // Clear
            _children.Clear();
        }

        internal override void InternalSetScene(Scene scene)
        {
            _scene = scene;
            foreach (SceneObject ch in Children)
                ch.InternalSetScene(scene);
        }

        internal override void InternalUnsetScene()
        {
            _scene = null;
            foreach (SceneObject ch in Children)
                ch.InternalUnsetScene();
        }

        public override void Unlock()
        {
            // Dummy, not needed here
        }

        internal static SceneObject CreateFromJson(string json)
        {
            JsonSceneObjectData? obj = JsonConvert.DeserializeObject<JsonSceneObjectData>(json);
            if (obj == null)
                throw new ArgumentException("Invalid JSON data");
            JsonSceneObject scO = new JsonSceneObject(obj);
            scO.AddChildren(obj);
            return scO;
        }

        internal override void RemoveChild(SceneObject child)
        {
            lock (_children)
                _children.Remove(child);
        }

        internal override void AddChild(SceneObject child)
        {
            lock (_children)
                _children.Add(child);
        }
    }
}
