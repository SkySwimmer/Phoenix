namespace Phoenix.Client.Components.Rules {

    /// <summary>
    /// Component load rule types
    /// </summary>
    public enum ComponentLoadRuleType {
        /// <summary>
        /// Dependency rule, requires the target component
        /// </summary>
        DEPENDENCY,

        /// <summary>
        /// Optional dependency rule, loads before the other component if present
        /// </summary>
        OPTDEPEND,

        /// <summary>
        /// Load-after rule, loads the invoking component before the target component
        /// </summary>
        LOADAFTER,

        /// <summary>
        /// Component that will conflict with the invoking component if it is loaded, crashing the client with a warning
        /// </summary>
        CONFLICT
    }

    /// <summary>
    /// Component loading rule
    /// </summary>
    public class ComponentLoadRule {
        private ComponentLoadRuleType _type;
        private string _componentID;

        /// <summary>
        /// Instantiates the load rule
        /// </summary>
        /// <param name="target">Target component</param>
        /// <param name="type">Load rule</param>
        public ComponentLoadRule(string target, ComponentLoadRuleType type) {
            _componentID = target;
            _type = type;
        }

        /// <summary>
        /// Target component ID
        /// </summary>
        public string Target
        {
            get
            {
                return _componentID;
            }
        }

        /// <summary>
        /// Component rule type
        /// </summary>
        public ComponentLoadRuleType Type
        {
            get
            {
                return _type;
            }
        }
    }

}