﻿namespace Phoenix.Common.AsyncTasks
{
    internal class AsyncTaskThread
    {
        private bool _available = false;
        public bool IsAvailable
        {
            get
            {
                return _available;
            }
        }

        public void Run()
        {
            while (true)
            {
                _available = true;

                // Wait for a task
                AsyncTask? tsk = null;
                for (int i = 0; i < 30000; i++)
                {
                    tsk = AsyncTaskManager.ObtainNext();
                    if (tsk != null)
                        break;
                    Thread.Sleep(1);
                }

                // No longer available
                _available = false;

                // If no task was selected after 30 seconds, exit
                if (tsk == null)
                    break;

                // Run task
                tsk.Run();
            }
        }
    }
}
