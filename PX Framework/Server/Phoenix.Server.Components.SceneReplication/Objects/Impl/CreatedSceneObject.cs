using Newtonsoft.Json;
using Phoenix.Common.Logging;
using Phoenix.Server.SceneReplication.Coordinates;
using Phoenix.Server.SceneReplication.Data;
using System.Reflection;

namespace Phoenix.Server.SceneReplication.Impl
{
    internal class CreatedSceneObject : SceneObject
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

        public CreatedSceneObject(string name, Transform transform, bool active, ReplicationDataMap data)
        {
            // Load fields
            _name = name;
            _path = name;
            _active = active;
            _replicating = true;

            // Load transform
            _transform = new(new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z, false), new Vector3(transform.Scale.X, transform.Scale.Y, transform.Scale.Z, false), new Vector3(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, false));
            _transform.OnReplicate += prop =>
            {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                CallOnReplicate(this, ReplicatingProperty.TRANSFORM, null, Transform);
            };

            // Load data
            _data = new ReplicationDataMap(new Dictionary<string, object?>(data.data), false);
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

            // Set ID
            _id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString("x2") + (Guid.NewGuid().ToString().Replace("-", ""));
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

        internal static SceneObject CreateFrom(string name, Transform transform, bool active, ReplicationDataMap data)
        {
            return new CreatedSceneObject(name, transform, active, data);
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
