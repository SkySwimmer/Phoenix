
using Phoenix.Common.Events;

namespace Phoenix.Server.Components.PlayerManager
{
    internal class DependencyTools
    {
        public static void LoadServerListPublisherSupport(PlayerManagerComponent comp)
        {
            comp.EventBus.AttachAll(new Support.ServerListSupportHandlers(comp));
        }
    }
}
