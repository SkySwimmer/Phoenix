using Phoenix.Common.Services;
using System.Diagnostics;

namespace Phoenix.Common.Tasks
{
    /// <summary>
    /// Scheduled task information object
    /// </summary>
    public class ScheduledTask
    {
        internal ScheduledTask() { }
        internal Action Action;
        internal bool _ran = false;

        internal long TimeStart;
        internal long MillisWait = -1;

        /// <summary>
        /// Checks if the task completed at least once
        /// </summary>
        public bool HasCompleted
        {
            get
            {
                return _ran;
            }
        }

        /// <summary>
        /// Blocks the current thread until the task has been run at least once
        /// </summary>
        public void Wait()
        {
            while (!_ran)
                Thread.Sleep(10);
        }

        internal int Interval = 0;
        internal int Limit = 1;

        internal int _CInterval = 0;
        internal int _CCount = 0;

        /// <summary>
        /// Retrieves the action tick interval (how many ticks before the action is run)
        /// </summary>
        public int ActionInterval
        {
            get
            {
                return Interval;
            }
        }

        /// <summary>
        /// Retrieves how often the action can run
        /// </summary>
        public int ActionLimit
        {
            get
            {
                return Limit;
            }
        }

        /// <summary>
        /// Retrieves the amount of remaining ticks before the action stops running
        /// </summary>
        public int TicksRemaining
        {
            get
            {
                if (Limit == -1)
                    return -1;
                return Limit - _CCount;
            }
        }

        /// <summary>
        /// Retrieves the amount of ticks that remain before the action is run
        /// </summary>
        public int TickBeforeStart
        {
            get
            {
                if (Interval <= 0 || _CCount >= Limit)
                    return 0;
                return Interval - _CInterval;
            }
        }
    }

    /// <summary>
    /// Task scheduling service
    /// </summary>
    public class TaskManager : IService
    {
        // Action list
        private List<ScheduledTask> Actions = new List<ScheduledTask>();

        /// <summary>
        /// Schedules and action that will run after the given amount of seconds have passed
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <param name="time">Time delay in seconds</param>
        /// <returns>ScheduledTask instance</returns>
        public ScheduledTask AfterSecs(Action action, int time)
        {
            return AfterMs(action, time * 1000);
        }

        /// <summary>
        /// Schedules and action that will run after the given amount of milliseconds have passed
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <param name="time">Time delay in milliseconds</param>
        /// <returns>ScheduledTask instance</returns>
        public ScheduledTask AfterMs(Action action, long time)
        {
            ScheduledTask acInfo = new ScheduledTask();
            acInfo.Action = action;
            acInfo.TimeStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            acInfo.MillisWait = time;
            lock (Actions)
                Actions.Add(acInfo);
            return acInfo;
        }

        /// <summary>
        /// Schedules a action that runs only once
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <returns>ActionInfo instance</returns>
        public ScheduledTask Oneshot(Action action)
        {
            ScheduledTask acInfo = new ScheduledTask();
            acInfo.Action = action;
            lock (Actions)
                Actions.Add(acInfo);
            return acInfo;
        }

        /// <summary>
        /// Schedules a action that runs only once after a specific amount of ticks have passed
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <returns>ActionInfo instance</returns>
        public ScheduledTask Delayed(Action action, int delay)
        {
            ScheduledTask acInfo = new ScheduledTask();
            acInfo.Interval = delay;
            acInfo.Action = action;
            lock (Actions)
                Actions.Add(acInfo);
            return acInfo;
        }

        /// <summary>
        /// Schedules a action that runs on a interval
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <param name="interval">Ticks to wait each time before running the action</param>
        /// <returns>ActionInfo instance</returns>
        public ScheduledTask Interval(Action action, int interval)
        {
            ScheduledTask acInfo = new ScheduledTask();
            acInfo.Interval = interval;
            acInfo.Limit = -1;
            acInfo.Action = action;
            lock (Actions)
                Actions.Add(acInfo);
            return acInfo;
        }

        /// <summary>
        /// Schedules a action that runs on a interval (only a specific amount of times)
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <param name="interval">Ticks to wait each time before running the action</param>
        /// <param name="limit">The amount of times to run this task</param>
        /// <returns>ActionInfo instance</returns>
        public ScheduledTask Interval(Action action, int interval, int limit)
        {
            ScheduledTask acInfo = new ScheduledTask();
            acInfo.Interval = interval;
            acInfo.Limit = limit;
            acInfo.Action = action;
            lock (Actions)
                Actions.Add(acInfo);
            return acInfo;
        }

        /// <summary>
        /// Schedules a action that runs on every tick
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <returns>ActionInfo instance</returns>
        public ScheduledTask Repeat(Action action)
        {
            ScheduledTask acInfo = new ScheduledTask();
            acInfo.Limit = -1;
            acInfo.Action = action;
            lock (Actions)
                Actions.Add(acInfo);
            return acInfo;
        }

        /// <summary>
        /// Schedules a action that runs on every tick until its limit is reached
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <param name="limit">The amount of times to run this task</param>
        /// <returns>ActionInfo instance</returns>
        public ScheduledTask Repeat(Action action, int limit)
        {
            ScheduledTask acInfo = new ScheduledTask();
            acInfo.Limit = limit;
            acInfo.Action = action;
            lock (Actions)
                Actions.Add(acInfo);
            return acInfo;
        }

        /// <summary>
        /// Cancels a action
        /// </summary>
        /// <param name="action">Action to cancel</param>
        public void Cancel(ScheduledTask action)
        {
            lock (Actions)
            {
                if (Actions.Contains(action))
                    Actions.Remove(action);
            }
        }

        /// <summary>
        /// Ticks the schedule, running any pending tasks
        /// </summary>
        public void Tick()
        {
            // Load actions
            List<ScheduledTask> actions;
            while (true)
            {
                try
                {
                    actions = new List<ScheduledTask>(Actions);
                    break;
                }
                catch
                {

                }
            }

            // Run actions
            foreach (ScheduledTask ac in actions)
            {
                if (ac == null || (ac.MillisWait != -1 && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ac.TimeStart < ac.MillisWait) || (ac.Interval != -1 && ac._CInterval++ < ac.Interval))
                    continue;

                // Run the action
                if (!Debugger.IsAttached)
                    try
                    {
                        ac.Action();
                    }
                    catch
                    {
                    }
                else
                    ac.Action();
                ac._ran = true;

                // Reset
                ac._CInterval = 0;

                // Increase count
                if (ac.Limit != -1)
                    ac._CCount++;

                // Remove if needed
                if (ac.Limit != -1 && ac._CCount >= ac.Limit)
                    lock(Actions)
                        Actions.Remove(ac);
            }
        }
    }
}
