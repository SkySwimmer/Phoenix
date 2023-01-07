using Phoenix.Server.Components;

namespace Phoenix.Server.Packages
{
    /// <summary>
    /// Core server package, contains absolute basic server components
    /// </summary>
    public class CorePackage : IComponentPackage
    {
        public string ID => "core";

        private Component[] _components = new Component[]
        {
            new TaskManagerComponent(),
            new ConfigManagerComponent()
        };

        public Component[] Components => _components;
    }
}
