using Phoenix.Common.Logging;
using Phoenix.Server.SceneReplication.Coordinates;
using Phoenix.Server.SceneReplication.Data;
using System.Reflection;

namespace Phoenix.Server.SceneReplication.Impl
{
    internal class ReflectingSceneObject : SceneObject
    {
        private bool unlocked;
        private object locker = new object();

        private SceneObject _original;
        private SceneObject? _parent;
        private Scene? _scene;

        private string _originalPath;
        
        private List<SceneObject> _children = new List<SceneObject>();

        private string _id;
        private string _path;
        private string _name;

        private bool _active;
        private bool _replicating;

        private Transform _transform;
        private ReplicationDataMap _data;


        public ReflectingSceneObject(SceneObject original, SceneObject? parent)
        {
            // Load fields
            _original = original;
            _path = _original.Path;
            _originalPath = _path;
            _replicating = _original.Replicating;

            // Set ID
            _id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString("x2") + (Guid.NewGuid().ToString().Replace("-", ""));

            // Add child objects
            foreach (SceneObject ch in original.Children)
            {
                _children.Add(new ReflectingSceneObject(ch, this));
            }
        }

        private void Copy()
        {
            // Load fields
            _name = _original.Name;
            _active = _original.Active;

            // Load transform
            _transform = new(new Vector3(_original.Transform.Position.X, _original.Transform.Position.Y, _original.Transform.Position.Z, !_replicating), new Vector3(_original.Transform.Scale.X, _original.Transform.Scale.Y, _original.Transform.Scale.Z, !_replicating), new Vector3(_original.Transform.Rotation.X, _original.Transform.Rotation.Y, _original.Transform.Rotation.Z, !_replicating));
            _transform.OnReplicate += prop =>
            {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                CallOnReplicate(this, ReplicatingProperty.TRANSFORM, null, Transform);
            };

            // Load data
            _data = new ReplicationDataMap(_original.ReplicationData.data, !_replicating);
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

            // Add components
            foreach (AbstractObjectComponent comp in _original.Components)
            {
                // Found it
                try
                {
                    ConstructorInfo? constr = comp.GetType().GetConstructor(new Type[0]);
                    if (constr == null)
                        throw new ArgumentException("No parameterless constructor");
                    object compInst = constr.Invoke(new object[0]);
                    if (compInst is AbstractObjectComponent)
                    {
                        Dictionary<string, object> data = new Dictionary<string, object>();
                        comp.Serialize(data);
                        AddComponent((AbstractObjectComponent)compInst).Deserialize(data);
                    }
                    else
                    {
                        throw new ArgumentException("Instantiated object is not an object component");
                    }
                }
                catch (Exception e)
                {
                    Logger.GetLogger("scene-manager").Error("Failed to add component " + comp.GetType().FullName + " to object " + _id + " (" + _path + ")", e);
                }
            }
        }

        public override bool Active
        {
            get
            {
                if (unlocked)
                    return _active;
                return _original.Active;
            }

            set
            {
                if (!unlocked)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                bool last = _active;
                _active = value;
                OnChangeActiveState(last);
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
                if (unlocked)
                    return _path;
                return _original.Path;
            }
        }

        public override string Name
        {
            get
            {
                if (unlocked)
                    return _name;
                return _original.Name;
            }

            set
            {
                if (!unlocked)
                    throw new ArgumentException("Cannot change properties of a read-only object");
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
                if (!unlocked)
                    throw new ArgumentException("Cannot change properties of a read-only object");
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
                if (!unlocked)
                    throw new ArgumentException("Cannot change properties of a read-only object");
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
                if (unlocked)
                    return _transform;
                return new Transform(new Vector3(_original.Transform.Position.X,
                        _original.Transform.Position.Y,
                        _original.Transform.Position.Z,
                        true
                    ), new Vector3(_original.Transform.Scale.X,
                        _original.Transform.Scale.Y,
                        _original.Transform.Scale.Z,
                        true
                    ), new Vector3(_original.Transform.Rotation.X,
                        _original.Transform.Rotation.Y,
                        _original.Transform.Rotation.Z,
                        true
                    ), true);
            }
        }

        public override ReplicationDataMap ReplicationData
        {
            get
            {
                if (!unlocked)
                    return _original.ReplicationData.ReadOnlyCopy();
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

        public override string OriginalPath => _originalPath;

        public override void Unlock()
        {
            if (unlocked)
                return;
            lock (locker)
            {
                if (!_original.Replicating)
                    throw new ArgumentException("Cannot unlock objects that do not replicate");
                Copy();
                unlocked = true;
                _original = null;
            }
        }

        public override void Destroy()
        {
            if (!unlocked)
                throw new ArgumentException("Cannot change properties of a read-only object");
            base.Destroy();
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

        internal override void AddChild(SceneObject child)
        {
            lock (_children)
                _children.Add(child);
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

        internal override void RemoveChild(SceneObject child)
        {
            lock (_children)
                _children.Remove(child);
        }
    }
}
