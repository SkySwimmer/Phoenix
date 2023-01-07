using Phoenix.Common.Logging;
using Phoenix.Common.Services;
using Phoenix.Server.Components.PlayerManager.Impl;

namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player Data Management Service
    /// </summary>
    public class PlayerDataService : IService
    {
        private List<PlayerDataFixer> _fixers = new List<PlayerDataFixer>();
        private List<PlayerDataProvider> _providers = new List<PlayerDataProvider>();

        public PlayerDataService()
        {
            AddProvider(new NoOperationPlayerDataProvider());
        }

        /// <summary>
        /// Adds data providers
        /// </summary>
        /// <param name="provider">Provider to add</param>
        public void AddProvider(PlayerDataProvider provider)
        {
            _providers.Insert(0, provider);
        }

        /// <summary>
        /// Adds player data fixers
        /// </summary>
        /// <param name="fixer">Fixer to add</param>
        public void AddDataFixer(PlayerDataFixer fixer)
        {
            _fixers.Add(fixer);
        }

        /// <summary>
        /// Deletes player data
        /// </summary>
        /// <param name="id">Player ID</param>
        public void DeletePlayerData(string id)
        {
            foreach (PlayerDataProvider provider in _providers)
            {
                if (provider.HasPlayerData(id))
                {
                    provider.DeletePlayerData(id);
                }
            }
        }

        /// <summary>
        /// Retrieves player data
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>PlayerDataContainer instance</returns>
        public PlayerDataContainer GetPlayerData(string id)
        {
            // First find the container
            PlayerDataContainer? result = null;
            PlayerDataProvider? selectedProvider = null;
            foreach (PlayerDataProvider provider in _providers)
            {
                if (provider.HasPlayerData(id))
                {
                    selectedProvider = provider;
                    result = provider.Provide(id);
                    break;
                }
            }
            if (result == null)
            {
                foreach (PlayerDataProvider provider in _providers)
                {
                    if (provider.CanUseAsFallback())
                    {
                        selectedProvider = provider;
                        result = provider.Provide(id);
                        break;
                    }
                }
            }
            if (result == null || selectedProvider == null)
                throw new InvalidOperationException("No data provider could provide the player data, this is a Phoenix Framework bug");

            // Check data version
            if (result.DataMinorVersion != selectedProvider.GetCurrentMinorDataVersion() || result.DataMajorVersion != selectedProvider.GetCurrentMajorDataVersion())
            {
                // Log
                Logger.GetLogger("player-data-manager").Info("Updating player data for " + id + "... This may take a while...");

                // Update player data
                while (result.DataMinorVersion != selectedProvider.GetCurrentMinorDataVersion() || result.DataMajorVersion != selectedProvider.GetCurrentMajorDataVersion())
                {
                    bool foundFixers = false;

                    // Find minor version fixers
                    foreach (PlayerDataFixer fixer in _fixers)
                    {
                        if (fixer.DataVersionMajor == result.DataMajorVersion && fixer.DataVersionMinor == result.DataMinorVersion)
                        {
                            foundFixers = true;
                            fixer.Fix(result);
                            break;
                        }
                    }
                    if (foundFixers)
                    {
                        result.DataMinorVersion++;
                        continue;
                    }
                    else
                    {
                        // Check if there are any fixers in the current version
                        foreach (PlayerDataFixer fixer in _fixers)
                        {
                            if (fixer.DataVersionMajor == result.DataMajorVersion && fixer.DataVersionMinor > result.DataMinorVersion)
                            {
                                foundFixers = true;
                                break;
                            }
                        }
                        if (foundFixers)
                        {
                            result.DataMinorVersion++;
                            continue;
                        }
                    }

                    // Find fixers for the current major version
                    if (result.DataMajorVersion == selectedProvider.GetCurrentMajorDataVersion())
                        result.DataMinorVersion++;
                    else
                    {
                        result.DataMajorVersion++;
                        result.DataMinorVersion = 0;
                    }
                }
                result.Save();
            }

            // Return result
            return result;
        }
    }
}
