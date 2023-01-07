namespace ChartZ.Engine {

    /// <summary>
    /// Chart command
    /// </summary>
    public abstract class ChartCommand {

        /// <summary>
        /// Defines the command ID string
        /// </summary>
        public abstract string CommandID { get; }

        /// <summary>
        /// Handles the command
        /// </summary>
        /// <param name="chain">Chart chain</param>
        /// <param name="segment">Command segment</param>
        /// <returns>True if successful, false otherwise</returns>
        public abstract bool Handle(ChartChain chain, ChartSegment segment);

    }

}