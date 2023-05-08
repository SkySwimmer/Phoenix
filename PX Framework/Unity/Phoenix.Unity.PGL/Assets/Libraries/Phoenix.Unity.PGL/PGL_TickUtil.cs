using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Phoenix.Unity.PGL.Internal
{
    public class PGL_TickUtil : MonoBehaviour
    {
        public class Task
        {
            internal Action action;
            internal bool completed;

            /// <summary>
            /// Checks if the task has been run
            /// </summary>
            public bool HasCompleted
            {
                get
                {
                    return completed;
                }
            }
        }
        private static List<Task> tasks = new List<Task>();
        internal static Action cleanupAction;

        public void OnDestroy()
        {
            if (cleanupAction != null)
                cleanupAction();
        }

        /// <summary>
        /// Schedules a task to run on Unity's FixedUpdate
        /// </summary>
        /// <param name="action">Task to run on Unity's FixedUpdate</param>
        /// <returns>Task object for checking the status</returns>
        public static Task Schedule(Action action)
        {
            Task tsk = new Task()
            {
                action = action
            };
            tasks.Add(tsk);
            return tsk;
        }

        /// <summary>
        /// Schedules a task to run on Unity's FixedUpdate and waits for it to complete (blocks the current thread)
        /// </summary>
        /// <param name="action">Task to run on Unity's FixedUpdate thread</param>
        public static void ScheduleAndWait(Action action)
        {
            Task tsk = new Task()
            {
                action = action
            };
            tasks.Add(tsk);
            while (!tsk.completed)
                Thread.Sleep(100);
        }

        public void FixedUpdate()
        {
            // Tick PGL
            PhoenixPGL.Tick();

            // Handle tasks
            Task[] tasks;
            while (true)
            {
                try
                {
                    tasks = PGL_TickUtil.tasks.ToArray();
                    break;
                }
                catch { }
            }
            foreach (Task tsk in tasks)
            {
                try
                {
                    tsk.action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                tsk.completed = true;
                PGL_TickUtil.tasks.Remove(tsk);
            }
        }
    }
}
