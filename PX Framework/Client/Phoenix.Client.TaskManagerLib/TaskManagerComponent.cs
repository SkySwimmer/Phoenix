using Phoenix.Common.Services;

namespace Phoenix.Client.Components
{
    /// <summary>
    /// Task Manager Component - Binds the TaskManager service to the client
    /// </summary>
    public class TaskManagerComponent : ClientComponent
    {
        public override string ID => "task-manager";

        protected override void Define()
        {
        }

        public override void PreInit()
        {
            ServiceManager.RegisterService(new Common.Tasks.TaskManager());
        }

        public override void Tick()
        {
            ServiceManager.GetService<Common.Tasks.TaskManager>().Tick();
        }
    }
}
