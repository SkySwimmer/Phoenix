using Phoenix.Common.Events;
using Phoenix.Server.Events;
using Phoenix.Server.Players;

namespace Phoenix.Server.Components.PlayerManager.Support
{
    public class ServerListSupportHandlers : IEventListenerContainer
    {
        private PlayerManagerComponent _component;
        public ServerListSupportHandlers(PlayerManagerComponent component)
        {
            _component = component;
        }

        [EventListener]
        public void OnUpdateList(ServerListUpdateEvent ev)
        {
            PlayerManagerService service = _component.ServiceManager.GetService<PlayerManagerService>();

            // Add current player count
            ev.DetailBlock.Set("players.current", service.Players.Length.ToString());
         
            // Add max count
            if (service.EnablePlayerLimit)
                ev.DetailBlock.Set("players.max", service.PlayerLimit.ToString());
            
            // Add snippet if enabled
            if (!_component.Configuration.HasEntry("add-player-name-snippet"))
                _component.Configuration.SetBool("add-player-name-snippet", false);
            if (_component.Configuration.GetBool("add-player-name-snippet"))
            {
                // Build snippet
                string snippet = "";
                foreach (Player plr in service.Players)
                {
                    if (snippet == "")
                        snippet = plr.DisplayName;
                    else
                        snippet += ", " + plr.DisplayName;
                }
                ev.DetailBlock.Set("players", snippet);
            }
        }
    }
}
