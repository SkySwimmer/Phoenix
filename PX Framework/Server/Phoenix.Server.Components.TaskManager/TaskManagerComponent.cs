using Phoenix.Common.Services;

namespace Phoenix.Server.Components
{
    /// <summary>
    /// Task Manager Component - Binds the TaskManager service to the server
    /// </summary>
    public class TaskManagerComponent : ServerComponent
    {
        public override string ID => "task-manager";

        protected override string ConfigurationKey => throw new NotImplementedException();

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
