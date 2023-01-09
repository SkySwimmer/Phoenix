using Phoenix.Common.Logging;
using System.Reflection;

namespace Phoenix.Common.Events
{
    /// <summary>
    /// The event bus - a system to listen to and dispatch events
    /// </summary>
    public class EventBus
    {
        private Dictionary<string, List<EventInfo>> _listeners = new Dictionary<string, List<EventInfo>>();

        private class EventInfo
        {
            public MethodInfo Method;
            public object Parent;
        }

        /// <summary>
        /// Dispatches a event
        /// </summary>
        /// <param name="ev">Event to dispatch</param>
        public void Dispatch(IEvent ev)
        {
            EventInfo[] mths;
            while (true)
            {
                try
                {
                    if (!_listeners.ContainsKey(ev.GetType().FullName))
                        mths = new EventInfo[0];
                    else
                        mths = _listeners[ev.GetType().FullName].ToArray();
                    break;
                }
                catch
                {
                }
            }

            foreach (EventInfo mth in mths)
            {
                try
                {
                    mth.Method.Invoke(mth.Parent, new object[] { ev });
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException != null)
                        Logger.GetLogger("event-bus").Error("Exception in event handler " + mth.Parent.GetType().Name, e.InnerException);
                }
                catch (Exception e)
                {
                    Logger.GetLogger("event-bus").Error("Exception in event handler " + mth.Parent.GetType().Name, e);
                }
                if (!ev.ShouldContinue())
                    break;
            }
        }

        /// <summary>
        /// Attaches all listeners in a event listener container
        /// </summary>
        /// <param name="container">IEventListenerContainer instance</param>
        public void AttachAll(IEventListenerContainer container)
        {
            Logger.GetLogger("event-bus").Debug("Adding event listeners from: " + container.GetType().FullName);
            foreach (MethodInfo meth in container.GetType().GetMethods())
            {
                EventListenerAttribute attr = meth.GetCustomAttribute<EventListenerAttribute>();
                if (attr != null && !meth.IsStatic && !meth.IsAbstract && meth.GetParameters().Length == 1 && typeof(IEvent).IsAssignableFrom(meth.GetParameters()[0].ParameterType))
                {
                    if (!_listeners.ContainsKey(meth.GetParameters()[0].ParameterType.FullName))
                        _listeners[meth.GetParameters()[0].ParameterType.FullName] = new List<EventInfo>();
                    _listeners[meth.GetParameters()[0].ParameterType.FullName].Add(new EventInfo() { 
                        Method = meth,
                        Parent = container
                    });
                }
            }
        }
    }
}
