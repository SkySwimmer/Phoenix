namespace Phoenix.Common.Events
{
    /// <summary>
    /// Event interface
    /// </summary>
    public abstract class IEvent
    {
        /// <summary>
        /// Checks if the event should continue execution
        /// </summary>
        /// <returns>True if the event should continue, false otherwise</returns>
        public virtual bool ShouldContinue() 
        {
            return true;
        }
    }
}
