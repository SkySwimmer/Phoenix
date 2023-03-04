using Newtonsoft.Json;
using Phoenix.Common.Logging;
using Phoenix.Server.SceneReplication.Coordinates;
using Phoenix.Server.SceneReplication.Data;

namespace Phoenix.Server.SceneReplication.Old
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
    internal class JsonSceneObject
    {
        public string name;
        public bool replicating;
        public bool active;

        public JsonTransform transform;
        public Dictionary<string, object?> replication = new Dictionary<string, object?>();

        public List<JsonSceneObject> children = new List<JsonSceneObject>();
    }

    public enum ReplicatingProperty
    {
        NAME,
        IS_ACTIVE,
        TRANSFORM_POSITION,
        TRANSFORM_ROTATION,
        TRANSFORM_SCALE,
        REPLICATION_DATA,
        REPLICATION_DATA_REMOVEKEY
    }

    /// <summary>
    /// Scene objects
    /// </summary>
    public abstract class SceneObject
    {
        protected string? room;
        protected SceneManager? manager;

        public delegate void DestroyHandler(SceneObject sender);
        public delegate void ChangeSceneHandler(SceneObject sender, Scene? oldScene, Scene? newScene);
        public delegate void ReParentHandler(SceneObject sender, SceneObject? oldParent, SceneObject? newParent);
        public delegate void ReplicationHandler(SceneObject sender, ReplicatingProperty property, object? value, string? key = null);
        protected string _originalPath;
        protected string _id = "undefined";

        /// <summary>
        /// Retrieves the object ID
        /// </summary>
        public string ID
        {
            get
            {
                return _id;
            }
        }

        internal string OriginalPath
        {
            get
            {
                return _originalPath;
            }
        }

        /// <summary>
        /// Reads a PRISM prefab file and adds it to the scene (uses the asset manager)
        /// </summary>
        /// <param name="filePath">Prefab path (without .prpm)</param>
        /// <returns>SceneObject instance</returns>
        public SceneObject SpawnPrefab(string filePath)
        {
            string assetPath = "SceneReplication/" + filePath + ".prpm";
            string prefabData;
            try
            {
                prefabData = AssetManager.GetAssetString(assetPath);
            }
            catch
            {
                throw new ArgumentException("Prefab not found: " + filePath);
            }
            SceneObject obj = SceneObject.FromJson(prefabData, manager, room);
            AddChild(obj);
            if (manager != null && room != null && Scene != null)
                manager.ReplicateAddPrefab(obj, filePath, room, Scene, this);
            return obj;
        }

        internal SceneObject(SceneManager? manager, string? room)
        {
            this.manager = manager;
            this.room = room;
        }

        /// <summary>
        /// Creates a reflecting scene object
        /// </summary>
        /// <param name="original">Original object</param>
        /// <param name="parent">Parent object</param>
        /// <param name="manager">Scene manager</param>
        /// <param name="room">Room ID</param>
        /// <returns>SceneObject instance</returns>
        public static SceneObject Reflecting(SceneObject original, SceneObject? parent, SceneManager? manager, string? room)
        {
            return new ReflectingSceneObject(original, parent, manager, room);
        }

        /// <summary>
        /// De-serializes Scene Objects
        /// </summary>
        /// <param name="json">Serialized scene object</param>
        /// <returns>SceneObject instances</returns>
        public static SceneObject FromJson(string json)
        {
            JsonSceneObject? obj = JsonConvert.DeserializeObject<JsonSceneObject>(json);
            if (obj == null)
                throw new ArgumentException("Invalid JSON data");
            SceneObjectImpl scO = new SceneObjectImpl(obj, null, null);
            scO.AddChildren(obj);
            return scO;
        }

        /// <summary>
        /// De-serializes Scene Objects
        /// </summary>
        /// <param name="json">Serialized scene object</param>
        /// <param name="manager">Scene manager</param>
        /// <param name="room">Room ID</param>
        /// <returns>SceneObject instances</returns>
        internal static SceneObject FromJson(string json, SceneManager? manager, string? room)
        {
            JsonSceneObject? obj = JsonConvert.DeserializeObject<JsonSceneObject>(json);
            if (obj == null)
                throw new ArgumentException("Invalid JSON data");
            SceneObjectImpl scO = new SceneObjectImpl(obj, manager, room);
            scO.AddChildren(obj);
            return scO;
        }

        /// <summary>
        /// Retrieves the scene the object is in
        /// </summary>
        public abstract Scene? Scene { get; set; }

        /// <summary>
        /// Retrieves the object parent
        /// </summary>
        public abstract SceneObject? Parent { get; set; }

        /// <summary>
        /// Object path
        /// </summary>
        public abstract string Path { get; }

        /// <summary>
        /// Object name
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// Checks if the object replicates
        /// </summary>
        public abstract bool Replicates { get; }

        /// <summary>
        /// Defines if the object is active or not
        /// </summary>
        public abstract bool Active { get; set; }

        /// <summary>
        /// Destroys the object
        /// </summary>
        public abstract void Destroy();

        /// <summary>
        /// Destroys the object without replication lockouts
        /// </summary>
        internal abstract void DestroyForced();

        /// <summary>
        /// Called when this object or a child of it requires to be replicated (called when properties change)
        /// </summary>
        public event ReplicationHandler? OnReplicate;

        /// <summary>
        /// Called when this object or a child of it is destroyed
        /// </summary>
        public event DestroyHandler? OnDestroy;

        /// <summary>
        /// Called when this object or a child of it is reparented
        /// </summary>
        public event ReParentHandler? OnReparent;

        /// <summary>
        /// Called when this object or a child of it is moved to a different scene
        /// </summary>
        public event ChangeSceneHandler? OnChangeScene;

        /// <summary>
        /// Object transform
        /// </summary>
        public abstract Transform Transform { get; }

        /// <summary>
        /// Retrieves the replication data map
        /// </summary>
        public abstract ReplicationDataMap ReplicationData { get; }

        /// <summary>
        /// Retrieves the child objects of this scene object
        /// </summary>
        public abstract SceneObject[] Children { get; }

        /// <summary>
        /// Unlocks the object
        /// </summary>
        public abstract void Unlock();

        /// <summary>
        /// Retrieves child objects by name
        /// </summary>
        /// <param name="name">Child object name</param>
        /// <returns>SceneObject instance</returns>
        public SceneObject GetChild(string name)
        {
            SceneObject? res = GetChildOrNull(name);
            if (res == null)
                throw new ArgumentException("Object not found");
            return res;
        }

        /// <summary>
        /// Retrieves child objects by name
        /// </summary>
        /// <param name="name">Child object name</param>
        /// <returns>SceneObject instance or null</returns>
        public SceneObject? GetChildOrNull(string name)
        {
            if (name.Contains("/"))
            {
                string pth = name.Remove(name.IndexOf("/"));
                string ch = name.Substring(name.IndexOf("/") + 1);
                foreach (SceneObject obj in Children)
                {
                    if (obj.Name == pth)
                    {
                        SceneObject t = obj.GetChild(ch);
                        if (t != null)
                            return t;
                    }
                }
                return null;
            }
            foreach (SceneObject obj in Children)
            {
                if (obj.Name == name)
                    return obj;
            }
            return null;
        }

        /// <summary>
        /// Assigns the parent scene object without running any replication
        /// </summary>
        /// <param name="scene">New parent scene</param>
        internal abstract void SetScene(Scene? scene);

        /// <summary>
        /// Assigns the object path without running any replication
        /// </summary>
        /// <param name="path">New path</param>
        internal abstract void SetPath(string path);

        /// <summary>
        /// Assigns the parent object without running any replication
        /// </summary>
        /// <param name="parent">Parent object</param>
        internal abstract void SetParent(SceneObject? parent);

        /// <summary>
        /// Adds child objects without running any replication
        /// </summary>
        /// <param name="child">Child object to add</param>
        internal abstract void AddChild(SceneObject child);

        /// <summary>
        /// Removes child objects without running any replication
        /// </summary>
        /// <param name="child">Child object to remove</param>
        internal abstract void RemoveChild(SceneObject child);

        protected void CallOnReplicate(SceneObject sender, ReplicatingProperty property, object? value, string? key = null)
        {
            OnReplicate?.Invoke(sender, property, value, key);
            if (Parent != null)
                Parent.CallOnReplicate(sender, property, value, key);
            else if (Scene != null)
                Scene.HandleReplicate(sender, property, value, key);
        }

        protected void CallOnDestroy(SceneObject sender, SceneObject? p, Scene? sc)
        {
            OnDestroy?.Invoke(sender);
            if (p != null)
                p.CallOnDestroy(sender, p.Parent, sc);
            else if (sc != null)
                sc.HandleOnDestroy(sender);
        }

        protected void CallOnChangeScene(SceneObject sender, Scene? oldScene, Scene? newScene)
        {
            OnChangeScene?.Invoke(sender, oldScene, newScene);
            if (Parent != null)
                Parent.CallOnChangeScene(sender, oldScene, newScene);
            else if (Scene != null)
                Scene.HandleOnChangeScene(sender, oldScene, newScene);
        }

        protected void CallOnReparent(SceneObject sender, SceneObject? oldParent, SceneObject? newParent)
        {
            OnReparent?.Invoke(sender, oldParent, newParent);
            if (Parent != null)
                Parent.CallOnReparent(sender, oldParent, newParent);
            else if (Scene != null)
                Scene.HandleReparent(sender, oldParent, newParent);
        }

        public override string ToString()
        {
            return (Scene != null ? Scene.Name + ":" : "") + Path;
        }
    }

    internal class ReflectingSceneObject : SceneObjectImpl
    {
        private Scene? _originalScene;
        private SceneObject _data;
        private bool unlocked;

        internal ReflectingSceneObject(SceneObject original, SceneObject? parent, SceneManager? manager, string? room) : base(manager, room)
        {
            _data = original;
            _path = original.Path;
            _originalPath = original.Path;
            _parent = parent;

            // Add child objects
            foreach (SceneObject ch in original.Children)
            {
                _children.Add(new ReflectingSceneObject(ch, this, manager, room));
            }
        }

        internal override void SetScene(Scene? scene)
        {
            base.SetScene(scene);
            if (_originalScene == null)
                _originalScene = scene;
        }

        // Unlock
        public override void Unlock()
        {
            if (!_data.Replicates)
                throw new ArgumentException("Cannot unlock objects that do not replicate");
            if (unlocked)
                return;

            // Copy original
            _name = _data.Name;
            _replicating = true;
            _active = _data.Active;
            _transform = new(new Vector3(_data.Transform.Position.X, _data.Transform.Position.Y, _data.Transform.Position.Z), new Vector3(_data.Transform.Scale.X, _data.Transform.Scale.Y, _data.Transform.Scale.Z), new Vector3(_data.Transform.Rotation.X, _data.Transform.Rotation.Y, _data.Transform.Rotation.Z));
            _transform.OnReplicate += prop =>
            {
                switch (prop)
                {
                    case ReplicatingTransformProperty.POSITION:
                        CallOnReplicate(this, ReplicatingProperty.TRANSFORM_POSITION, Transform);
                        break;
                    case ReplicatingTransformProperty.ROTATION:
                        CallOnReplicate(this, ReplicatingProperty.TRANSFORM_ROTATION, Transform);
                        break;
                    case ReplicatingTransformProperty.SCALE:
                        CallOnReplicate(this, ReplicatingProperty.TRANSFORM_SCALE, Transform);
                        break;
                }
            };
            _replication = new ReplicationDataMap(JsonConvert.DeserializeObject<Dictionary<string,object>>(JsonConvert.SerializeObject(_data.ReplicationData.data)));
            _replication.OnChange += (key, val) => {
                CallOnReplicate(this, ReplicatingProperty.REPLICATION_DATA, val, key);
            };
            _replication.OnRemove += (key) => {
                CallOnReplicate(this, ReplicatingProperty.REPLICATION_DATA_REMOVEKEY, key);
            };

            // Unset data and unlock
            _data = null;
            unlocked = true;
        }

        public override string Name
        {
            get
            {
                if (unlocked)
                    return base.Name;
                return _data.Name;
            }

            set
            {
                if (!unlocked)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                base.Name = value;
            }
        }

        public override string Path
        {
            get
            {
                if (unlocked)
                    return base.Path;
                return _data.Path;
            }
        }

        public override bool Replicates
        {
            get
            {
                if (unlocked)
                    return base.Replicates;
                return _data.Replicates;
            }
        }

        public override bool Active
        {
            get
            {
                if (unlocked)
                    return base.Active;
                return _data.Active;
            }

            set
            {
                if (!unlocked)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                base.Active = value;
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
                base.Scene = value;
                if (_originalScene != null)
                    _originalScene._newObjectScenes[_originalPath] = Scene.Path;
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
                base.Parent = value;
                if (_originalScene != null)
                    _originalScene._reparentedObjects[_originalPath] = value;
            }
        }

        public override Transform Transform
        {
            get
            {
                if (!unlocked && _data.Replicates)
                    return new Transform(new Vector3(_data.Transform.Position.X,
                            _data.Transform.Position.Y,
                            _data.Transform.Position.Z,
                            true
                        ), new Vector3(_data.Transform.Scale.X,
                            _data.Transform.Scale.Y,
                            _data.Transform.Scale.Z,
                            true
                        ), new Vector3(_data.Transform.Rotation.X,
                            _data.Transform.Rotation.Y,
                            _data.Transform.Rotation.Z,
                            true
                        ), true);
                else if (!unlocked)
                    return _data.Transform;
                return base.Transform;
            }
        }

        public override ReplicationDataMap ReplicationData
        {
            get
            {
                if (!unlocked)
                    return _data.ReplicationData.ReadOnlyCopy();
                return base.ReplicationData;
            }
        }

        public override void Destroy()
        {
            if (!unlocked)
                throw new ArgumentException("Cannot change properties of a read-only object");
            base.Destroy();
            if (_originalScene != null)
            {
                _originalScene._destroyedObjects.Add(_originalPath);
                if (_originalScene._reparentedObjects.ContainsKey(_originalPath))
                    _originalScene._reparentedObjects.Remove(_originalPath);
                if (_originalScene._newObjectScenes.ContainsKey(_originalPath))
                    _originalScene._newObjectScenes.Remove(_originalPath);
            }
            _originalScene = null;
        }
    }

    internal class SceneObjectImpl : SceneObject
    {
        protected string _path;
        protected string _name;

        protected bool _replicating = false;
        protected bool _active = false;
        protected Transform _transform;
        protected ReplicationDataMap _replication;
        protected List<SceneObject> _children = new List<SceneObject>();
        protected SceneObject? _parent;
        internal Scene? _scene;

        internal SceneObjectImpl(SceneManager? manager, string? room) : base(manager, room) {}

        internal SceneObjectImpl(JsonSceneObject obj, SceneManager? manager, string? room) : base(manager, room)
        {
            _name = obj.name;
            _replicating = obj.replicating;
            _active = obj.active;
            _transform = new(new Vector3(obj.transform.position.x, obj.transform.position.y, obj.transform.position.z, !_replicating), new Vector3(obj.transform.scale.x, obj.transform.scale.y, obj.transform.scale.z, !_replicating), new Vector3(obj.transform.angles.x, obj.transform.angles.y, obj.transform.angles.z, !_replicating));
            _transform.OnReplicate += prop =>
            {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                switch (prop)
                {
                    case ReplicatingTransformProperty.POSITION:
                        CallOnReplicate(this, ReplicatingProperty.TRANSFORM_POSITION, Transform);
                        break;
                    case ReplicatingTransformProperty.ROTATION:
                        CallOnReplicate(this, ReplicatingProperty.TRANSFORM_ROTATION, Transform);
                        break;
                    case ReplicatingTransformProperty.SCALE:
                        CallOnReplicate(this, ReplicatingProperty.TRANSFORM_SCALE, Transform);
                        break;
                }
            };
            _replication = new ReplicationDataMap(obj.replication, !_replicating);
            _replication.OnChange += (key, val) => {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                CallOnReplicate(this, ReplicatingProperty.REPLICATION_DATA, val, key);
            };
            _replication.OnRemove += (key) => {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                CallOnReplicate(this, ReplicatingProperty.REPLICATION_DATA_REMOVEKEY, key);
            };
            _path = obj.name;
            _originalPath = _path;
        }

        internal void AddChildren(JsonSceneObject obj)
        {
            foreach (JsonSceneObject ch in obj.children)
            {
                SceneObjectImpl objC = new SceneObjectImpl(ch, manager, room);
                AddChild(objC);
                objC.AddChildren(ch);
            }
        }

        internal override void SetParent(SceneObject? parent)
        {
            _parent = parent;
        }

        internal override void SetScene(Scene? scene)
        {
            _scene = scene;
        }

        internal override void SetPath(string path)
        {
            _path = path;
        }

        internal override void AddChild(SceneObject objC)
        {
            objC.SetScene(_scene);
            objC.SetParent(this);
            objC.SetPath(_path + "/" + objC.Path);
            if (_replicating && _children.Any(t => t.Name == objC.Name))
                Logger.GetLogger("scene-replication").Warn("Replication conflict: " + objC.Path + " has multiple matches!");
            _children.Add(objC);
        }

        internal override void RemoveChild(SceneObject objC)
        {
            _children.Remove(objC);
        }

        /// <summary>
        /// Retrieves the scene the object is in
        /// </summary>
        public override Scene? Scene
        {
            get
            {
                return _scene;
            }
            set
            {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                if (value == null)
                    throw new ArgumentException("Null scene assignment unsupported as it breaks replication");
                if (_parent != null)
                    Parent = null;
                else if (_scene != null)
                    _scene.RemoveFromScene(this); // Only root nodes need to call this
                Scene? oldScene = _scene;
                _scene = value;
                _scene.AddToScene(this);
                CallOnChangeScene(this, oldScene, value);
            }
        }

        /// <summary>
        /// Retrieves the object parent
        /// </summary>
        public override SceneObject? Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");

                // Call re-parent
                SceneObject? oldParent = _parent;
                if (_parent != null)
                    _parent.RemoveChild(this);
                _parent = value;
                _path = _name;
                if (_parent != null)
                    _parent.AddChild(this);
                else if (_scene != null && !_scene.Objects.Contains(this))
                    _scene.AddToScene(this);
                CallOnReparent(this, oldParent, value);
            }
        }

        /// <summary>
        /// Destroys the object
        /// </summary>
        public override void Destroy()
        {
            if (!_replicating)
                throw new ArgumentException("Cannot change properties of a read-only object");
            DestroyForced();
        }

        internal override void DestroyForced()
        {
            if (_parent == null && _scene != null)
                _scene.RemoveFromScene(this);
            if (_parent != null)
                _parent.RemoveChild(this);
            Scene sc = _scene;
            SceneObject p = _parent;
            SetParent(null);
            SetScene(null);
            CallOnDestroy(this, p, sc);

            // Destroy children
            SceneObject[] chs = Children;
            _children.Clear();
            foreach (SceneObject obj in chs)
            {
                obj.SetParent(null);
                obj.SetScene(null);
                obj.DestroyForced();
            }
        }

        // Dummy
        public override void Unlock()
        {
            if (!_replicating)
                throw new ArgumentException("Cannot unlock objects that do not replicate");
        }

        /// <summary>
        /// Object path
        /// </summary>
        public override string Path
        {
            get
            {
                return _path;
            }
        }

        /// <summary>
        /// Object name
        /// </summary>
        public override string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                _name = value;
                CallOnReplicate(this, ReplicatingProperty.NAME, _name);
                _path = _parent == null ? _name : _parent.Path + "/" + _name;
            }
        }

        /// <summary>
        /// Checks if the object replicates
        /// </summary>
        public override bool Replicates
        {
            get
            {
                return _replicating;
            }
        }

        /// <summary>
        /// Defines if the object is active or not
        /// </summary>
        public override bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                if (!_replicating)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                _active = value;
                CallOnReplicate(this, ReplicatingProperty.IS_ACTIVE, value);
            }
        }
        /// <summary>
        /// Object transform
        /// </summary>
        public override Transform Transform
        {
            get
            {
                return _transform;
            }
        }

        /// <summary>
        /// Retrieves the replication data map
        /// </summary>
        public override ReplicationDataMap ReplicationData
        {
            get
            {
                return _replication;
            }
        }

        /// <summary>
        /// Retrieves the child objects of this scene object
        /// </summary>
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
    }
}
