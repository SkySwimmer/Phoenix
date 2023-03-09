using Phoenix.Common.Networking.Connections;
using Phoenix.Server.SceneReplication.Coordinates;
using Phoenix.Server.SceneReplication.Data;
using Phoenix.Server.SceneReplication.Impl;
using System.Reflection;

namespace Phoenix.Server.SceneReplication
{
    /// <summary>
    /// Types of replication properties
    /// </summary>
    public enum ReplicatingProperty
    {
        /// <summary>
        /// Object name
        /// </summary>
        NAME,

        /// <summary>
        /// Object active state
        /// </summary>
        IS_ACTIVE,

        /// <summary>
        /// Object transform
        /// </summary>
        TRANSFORM,

        /// <summary>
        /// Replication data
        /// </summary>
        REPLICATION_DATA,

        /// <summary>
        /// Removed replication data keys
        /// </summary>
        REPLICATION_DATA_REMOVEKEY
    }

    #region Event handler definitions

    /// <summary>
    /// Object destroy event handler
    /// </summary>
    /// <param name="sender">Object that was destroyed</param>
    public delegate void DestroyHandler(SceneObject sender);

    /// <summary>
    /// Scene change event handler
    /// </summary>
    /// <param name="sender">Object that was moved</param>
    /// <param name="oldScene">Old scene instance</param>
    /// <param name="newScene">New scene instance/param>
    public delegate void ChangeSceneHandler(SceneObject sender, Scene? oldScene, Scene? newScene);

    /// <summary>
    /// Object reparent event handler
    /// </summary>
    /// <param name="sender">Object that was reparented</param>
    /// <param name="oldParent">Old parent instance</param>
    /// <param name="newParent">New parent instance</param>
    public delegate void ReParentHandler(SceneObject sender, SceneObject? oldParent, SceneObject? newParent);

    /// <summary>
    /// Object replication event handler
    /// </summary>
    /// <param name="sender">Object that was replicated</param>
    /// <param name="property">Property that was changed</param>
    /// <param name="key">Optional key field</param>
    /// <param name="value">Optional value field</param>
    public delegate void ReplicationHandler(SceneObject sender, ReplicatingProperty property, string? key, object? value);

    #endregion
    /// <summary>
    /// Scene object
    /// </summary>
    public abstract class SceneObject
    {
        private List<AbstractObjectComponent> _components = new List<AbstractObjectComponent>();
        private Connection? _owningConnection;

        /// <summary>
        /// Retrieves the owning connection of this object (assigning this will prevent other clients from receiving and sending messages to this object)
        /// </summary>
        public Connection? OwningConnection
        {
            get
            {
                if (_owningConnection != null)
                    return _owningConnection;
                if (Parent != null)
                    return Parent.OwningConnection;
                return null;
            }

            set
            {
                if (_owningConnection != null)
                    _owningConnection.Disconnected -= DisconnectHandler;
                _owningConnection = value;
                if (_owningConnection != null)
                    _owningConnection.Disconnected += DisconnectHandler;
            }
        }

        private void DisconnectHandler(Connection connection, string reason, string[] args)
        {
            // Call for all components that do not have a custom handler
            foreach (AbstractObjectComponent comp in Components)
            {
                if (comp.OwningConnection == connection)
                    comp.Disconnect(reason, args);
            }

            // Call for child objects
            foreach (SceneObject obj in Children)
                obj.DisconnectHandler(connection, reason, args);
        }

        /// <summary>
        /// Active handler for the FromJson method
        /// </summary>
        protected static FromJsonHandler CreateFromJsonHandler = json =>
        {
            return JsonSceneObject.CreateFromJson(json);
        };

        /// <summary>
        /// Creates a reflecting scene object
        /// </summary>
        /// <param name="original">Original object</param>
        /// <param name="parent">Parent object</param>
        /// <returns>SceneObject instance</returns>
        public static SceneObject Reflecting(SceneObject original, SceneObject? parent)
        {
            return new ReflectingSceneObject(original, parent);
        }

        /// <summary>
        /// Handler for loading scene objects from json paylaods
        /// </summary>
        /// <param name="json">Json payload</param>
        /// <returns>SceneObject instance</returns>
        protected delegate SceneObject FromJsonHandler(string json);

        /// <summary>
        /// De-serializes Scene Objects
        /// </summary>
        /// <param name="json">Serialized scene object</param>
        /// <returns>SceneObject instances</returns>
        public static SceneObject FromJson(string json)
        {
            return CreateFromJsonHandler(json);
        }

        #region Fields

        /// <summary>
        /// Defines if the object is active
        /// </summary>
        public abstract bool Active { get; set; }

        /// <summary>
        /// Object ID
        /// </summary>
        public abstract string ID { get; }

        /// <summary>
        /// Object path
        /// </summary>
        public abstract string Path { get; }

        /// <summary>
        /// Object original path
        /// </summary>
        public virtual string OriginalPath { get { return Path; } }

        /// <summary>
        /// Object name
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// Checks if this object replicates or is read-only
        /// </summary>
        public abstract bool Replicating { get; }

        /// <summary>
        /// Removes child objects
        /// </summary>
        /// <param name="child">Child object to remove</param>
        internal abstract void RemoveChild(SceneObject child);

        /// <summary>
        /// Adds child objects
        /// </summary>
        /// <param name="child">Child object to add</param>
        internal abstract void AddChild(SceneObject child);

        /// <summary>
        /// Retrieves the parent object (may be null)
        /// </summary>
        public abstract SceneObject? Parent { get; set; }

        /// <summary>
        /// Retrieves the object transform
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
        /// Retrieves the owning scene
        /// </summary>
        public abstract Scene? Scene { get; set; }

        /// <summary>
        /// Assigns the scene instance without running any scene processing
        /// </summary>
        /// <param name="scene">Scene instance</param>
        internal abstract void InternalSetScene(Scene scene);

        /// <summary>
        /// Unsets the scene instance without running any scene processing
        /// </summary>
        internal abstract void InternalUnsetScene();

        #endregion
        #region Pre-defined fields and methods

        /// <summary>
        /// Retrieves all object components
        /// </summary>
        public AbstractObjectComponent[] Components
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return _components.ToArray();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Removes object components
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>True if removed, false otherwise</returns>
        public bool RemoveComponent<T>() where T : AbstractObjectComponent
        {
            foreach (AbstractObjectComponent comp in Components)
            {
                if (comp is T)
                {
                    comp.Destroy();
                    lock (_components)
                        _components.Remove(comp);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes object components
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="instance">Component instance</param>
        /// <returns>True if removed, false otherwise</returns>
        public bool RemoveComponent<T>(T instance) where T : AbstractObjectComponent
        {
            if (_components.Contains(instance))
            {
                lock (_components)
                    _components.Remove(instance);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds object components
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Component instance or existing instance if one is present</returns>
        public T AddComponent<T>() where T : AbstractObjectComponent
        {
            if (HasComponent<T>())
                return GetComponent<T>();
            try
            {
                // Find constructor
                ConstructorInfo? constr = typeof(T).GetConstructor(new Type[0]);
                if (constr == null)
                    throw new ArgumentException();

                // Create instance
                object inst = constr.Invoke(new object[0]);
                if (inst is T)
                {
                    T instance = (T)inst;
                    lock (_components)
                        _components.Add(instance);
                    instance.Setup(this);
                    return instance;
                }
                throw new ArgumentException();
            }
            catch
            {
                throw new ArgumentException("Component does not have an empty constructor");
            }
        }

        /// <summary>
        /// Adds object components
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="instance">Component instance</param>
        /// <returns>Component instance</returns>
        public T AddComponent<T>(T instance) where T : AbstractObjectComponent
        {
            lock (_components)
                _components.Add(instance);
            instance.Setup(this);
            return instance;
        }

        /// <summary>
        /// Checks if a component is present
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>True if present, false otherwise</returns>
        public bool HasComponent<T>() where T : AbstractObjectComponent
        {
            foreach (AbstractObjectComponent comp in Components)
            {
                if (comp is T)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves object components (throws an exception if not present)
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Component instance</returns>
        public T GetComponent<T>() where T : AbstractObjectComponent
        {
            foreach (AbstractObjectComponent comp in Components)
            {
                if (comp is T)
                    return (T)comp;
            }
            throw new ArgumentException("Component not present");
        }

        /// <summary>
        /// Retrieves all object components of a type
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Array of component instances</returns>
        public T[] GetComponents<T>() where T : AbstractObjectComponent
        {
            return Components.Where(t => t is T).Select(t => (T)t).ToArray();
        }

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
        /// <param name="name">Object name</param>
        /// <returns>Array of SceneObject instances</returns>
        public SceneObject[] GetChildren(string name)
        {
            if (name.Contains("/"))
            {
                string pth = name.Remove(name.IndexOf("/"));
                string ch = name.Substring(name.IndexOf("/") + 1);
                foreach (SceneObject obj in Children)
                {
                    if (obj.Name == pth)
                    {
                        SceneObject[] objs = obj.GetChildren(ch);
                        if (objs.Length != 0)
                            return objs;
                    }
                }
                return new SceneObject[0];
            }
            return Children.Where(t => t.Name == name).ToArray();
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
        /// Destroys the object
        /// </summary>
        public virtual void Destroy()
        {
            if (!Replicating)
                throw new ArgumentException("Cannot modify read-only objects");
            DestroyForced();
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
            SceneObject obj = SceneObject.FromJson(prefabData);
            if (Scene != null)
                Scene.HandleSpawnPrefab(filePath, obj, this);
            obj.Parent = this;
            return obj;
        }

        #endregion
        #region Internal methods

        /// <summary>
        /// Updates all components
        /// </summary>
        internal void Update()
        {
            foreach (AbstractObjectComponent comp in Components)
            {
                if (comp == null)
                    continue;
                comp.Update();
            }
            foreach (SceneObject ch in Children)
                ch.Update();
        }

        /// <summary>
        /// Calls destroy, should be called only if the object has not been destroyed before and before unsetting the parent and scene
        /// </summary>
        /// <param name="sender">Object that caused this object to be destroyed (eg. a parent or root)</param>
        protected void CallOnDestroy(SceneObject sender)
        {
            OnDestroy?.Invoke(sender);
            if (sender == this)
            {
                foreach (AbstractObjectComponent comp in Components)
                {
                    if (comp == null)
                        continue;
                    if (Scene != null)
                    {
                        if (Active)
                            comp.Disable();
                        comp.Stop();
                    }
                    comp.Destroy();
                }
            }
        }

        /// <summary>
        /// Calls events needed after destroy, should be called after unsetting the scene and parent
        /// </summary>
        /// <param name="sender">Object that caused this object to be destroyed (eg. a parent or root)</param>
        /// <param name="oldParent">Old parent object</param>
        /// <param name="oldScene">Old scene object</param>
        protected void PostCallDestroy(SceneObject sender, SceneObject? oldParent, Scene? oldScene)
        {
            if (oldParent != null)
            {
                oldParent.CallOnDestroy(sender);
                oldParent.PostCallDestroy(sender, oldParent == null ? null : oldParent.Parent, oldScene);
            }
            else if (oldScene != null)
                oldScene.HandleOnDestroy(sender);

            if (sender == this)
            {
                // Destroy child objects
                foreach (SceneObject ch in Children)
                {
                    ch.DestroyForced();
                }
            }
        }

        /// <summary>
        /// Calls replication
        /// </summary>
        /// <param name="sender">Object firing the event</param>
        /// <param name="property">Property type</param>
        /// <param name="key">Key field</param>
        /// <param name="value">Value field</param>
        protected void CallOnReplicate(SceneObject sender, ReplicatingProperty property, string? key, object? value)
        {
            OnReplicate?.Invoke(sender, property, key, value);
            if (Parent != null)
                Parent.CallOnReplicate(sender, property, key, value);
            else if (Scene != null)
                Scene.HandleReplicate(sender, property, key, value);
        }

        /// <summary>
        /// Calls scene change
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="oldScene">Old scene</param>
        /// <param name="newScene">New scene</param>
        protected void CallOnChangeScene(SceneObject sender, Scene? oldScene, Scene? newScene)
        {
            if (oldScene != null && sender == this)
            {
                foreach (AbstractObjectComponent comp in Components)
                {
                    if (comp == null)
                        continue;
                    comp.Stop();
                }
            }
            OnChangeScene?.Invoke(sender, oldScene, newScene);
            if (Parent != null)
                Parent.CallOnChangeScene(sender, oldScene, newScene);
            else
            {
                // Root
                if (oldScene != null)
                {
                    oldScene.HandleOnChangeScene(sender, oldScene, newScene);
                    oldScene.RemoveFromScene(sender);
                }
                if (newScene != null)
                    newScene.AddToScene(sender);
            }
            if (newScene != null && sender == this)
            {
                foreach (AbstractObjectComponent comp in Components)
                {
                    if (comp == null)
                        continue;
                    comp.Start();
                }
            }
        }

        /// <summary>
        /// Calls object reparent
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="oldParent">Old parent</param>
        /// <param name="newParent">new parent</param>
        protected void CallOnReparent(SceneObject sender, SceneObject? oldParent, SceneObject? newParent)
        {
            OnReparent?.Invoke(sender, oldParent, newParent);
            if (Parent != null)
                Parent.CallOnReparent(sender, oldParent, newParent);
            else if (Scene != null)
                Scene.HandleReparent(sender, oldParent, newParent);
        }

        /// <summary>
        /// Handles Active state changes for components (must be called AFTER changing the value of Active)
        /// </summary>
        /// <param name="lastState">Previous state</param>
        protected void OnChangeActiveState(bool lastState)
        {
            if (Active != lastState)
            {
                CallOnReplicate(this, ReplicatingProperty.IS_ACTIVE, null, Active);
                foreach (AbstractObjectComponent comp in Components)
                {
                    if (comp == null)
                        continue;
                    if (Active)
                        comp.Enable();
                    else
                        comp.Disable();
                }
            }
        }

        #endregion
        #region Abstract methods

        /// <summary>
        /// Unlocks the object for changing properties (only possible for non-read-only objects)
        /// </summary>
        public abstract void Unlock();

        /// <summary>
        /// Destroys the object without replication lockouts
        /// </summary>
        protected abstract void DestroyForced();

        #endregion
        #region Events

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

        #endregion

        public override string ToString()
        {
            return (Scene != null ? Scene.Name + ":" : "") + Path;
        }
    }
}
