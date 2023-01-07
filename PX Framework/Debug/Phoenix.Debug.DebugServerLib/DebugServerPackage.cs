using Phoenix.Server.Components;

namespace Phoenix.Debug.DebugServerLib
{
    public class DebugServerPackage : IComponentPackage
    {
        public string ID => "debug-server";

        public Component[] Components => new Component[] {
            new DebugServerComponent()
        };
    }
}
