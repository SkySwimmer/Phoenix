namespace Phoenix.Server.Components {

    /// <summary>
    /// Component package interface
    /// </summary>
    public interface IComponentPackage {
        /// <summary>
        /// Package ID
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// Components in the package
        /// </summary>
        public Component[] Components { get; }
    }

}