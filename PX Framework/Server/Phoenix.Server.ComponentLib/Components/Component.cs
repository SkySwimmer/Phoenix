using Phoenix.Common.Events;
using Phoenix.Common.Logging;
using Phoenix.Server.Components.Rules;

namespace Phoenix.Server.Components
{

    /// <summary>
    /// Component interface
    /// </summary>
    public abstract class Component : IEventListenerContainer
    {
        /// <summary>
        /// Component ID
        /// </summary>
        public abstract string ID { get; }

        /// <summary>
        /// Component alias IDs
        /// </summary>
        public virtual string[] Aliases { get { return new string[0]; } }

        // Fields
        private Logger logger;

        // Load rules
        private bool _defined = false;
        private List<ComponentLoadRule> _loadRules = new List<ComponentLoadRule>();

        /// <summary>
        /// Internal
        /// </summary>
        public void InitLoadRules()
        {
            if (_defined)
                return;

            Define();
            _defined = true;
        }

        /// <summary>
        /// Component load rules
        /// </summary>
        public ComponentLoadRule[] LoadRules
        {
            get
            {
                return _loadRules.ToArray();
            }
        }

        /// <summary>
        /// Adds a dependency definition
        /// </summary>
        /// <param name="component">Dependency component ID</param>
        protected void DependsOn(string component)
        {
            if (_defined)
                throw new InvalidOperationException("Called after component definition");
            _loadRules.Add(new ComponentLoadRule(component, ComponentLoadRuleType.DEPENDENCY));
        }

        /// <summary>
        /// Adds a optional dependency definition
        /// </summary>
        /// <param name="component">Dependency component ID</param>
        protected void OptDependsOn(string component)
        {
            if (_defined)
                throw new InvalidOperationException("Called after component definition");
            _loadRules.Add(new ComponentLoadRule(component, ComponentLoadRuleType.OPTDEPEND));
        }

        /// <summary>
        /// Adds a load-before definition (this component will be loaded before the target component)
        /// </summary>
        /// <param name="component">Dependency component ID</param>
        protected void LoadBefore(string component)
        {
            if (_defined)
                throw new InvalidOperationException("Called after component definition");
            _loadRules.Add(new ComponentLoadRule(component, ComponentLoadRuleType.LOADAFTER));
        }

        /// <summary>
        /// Adds a conflict definition
        /// </summary>
        /// <param name="component">Dependency component ID</param>
        protected void ConflictsWith(string component)
        {
            if (_defined)
                throw new InvalidOperationException("Called after component definition");
            _loadRules.Add(new ComponentLoadRule(component, ComponentLoadRuleType.CONFLICT));
        }

        /// <summary>
        /// Retrieves the component logger
        /// </summary>
        /// <returns>Component logger instance</returns>
        protected Logger GetLogger()
        {
            if (logger == null)
                logger = Logger.GetLogger(GetType().Name);
            return logger;
        }

        #region Abstracts

        /// <summary>
        /// Called immediately after the component is registered, use this for dependencies and other rules
        /// </summary>
        protected abstract void Define();

        /// <summary>
        /// Called when the component is loaded
        /// </summary>
        public virtual void PreInit() { }

        /// <summary>
        /// Called when initializing the component
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// Called after the server starts
        /// </summary>
        public virtual void StartServer() { }
        
        /// <summary>
        /// Called when the server stops
        /// </summary>
        public virtual void StopServer() { }

        /// <summary>
        /// Called on each server tick
        /// </summary>
        public virtual void Tick() { }

        #endregion

    }

}