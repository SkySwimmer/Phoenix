using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Impl;
using Phoenix.Common.Networking.Registry;
using Phoenix.Server;
using Phoenix.Server.IntegratedServerLib;

namespace Phoenix.Client.IntegratedServerBootstrapper
{
    /// <summary>
    /// Abstract class for Phoenix integrated servers
    /// </summary>
    public abstract class PhoenixIntegratedServer
    {
        private Logger logger = Logger.GetLogger("Phoenix Server Loader");
        private IntegratedServerConnection? server;
        private bool locked = false;
        private GameServer? srv;

        /// <summary>
        /// GameServer creation event handler
        /// </summary>
        /// <param name="server">GameServer instance that was just created</param>
        public delegate void GameServerCreatedHandler(GameServer server);

        /// <summary>
        /// Called when a game server is created
        /// </summary>
        public static event GameServerCreatedHandler? OnCreateServer;

        /// <summary>
        /// Creates a integrated client connection
        /// </summary>
        /// <param name="channels">Cannel registry</param>
        /// <returns>IntegratedClientConnection instance</returns>
        public IntegratedClientConnection CreateConnection(ChannelRegistry channels)
        {
            if (srv == null || server == null)
                throw new InvalidOperationException("Server not initialized");
            if (!srv.IsRunning())
                throw new InvalidOperationException("Server not running");
            IntegratedClientBundle bundle = IntegratedClientConnection.Create(channels, srv.ChannelRegistry);
            server.AddClient(bundle.ServerSide);
            return bundle.ClientSide;
        }

        /// <summary>
        /// Retrieves the server connection made for the integrated server
        /// </summary>
        public IntegratedServerConnection ServerConnection
        {
            get
            {
                if (server == null)
                    throw new InvalidOperationException("Server not initialized");
                return server;
            }
        }

        /// <summary>
        /// Retrieves the game server instance
        /// </summary>
        public GameServer GameServer
        {
            get
            {
                if (srv == null)
                    throw new InvalidOperationException("Server not initialized");
                return srv;
            }
        }

        /// <summary>
        /// Initializes the server
        /// </summary>
        public void Init()
        {
            if (locked)
                throw new InvalidOperationException("Already initialized");

            // Prepare
            logger.Info("Preparing integrated server...");
            Prepare();

            // Create servers
            srv = SetupServer();
            if (srv.ChannelRegistry == null)
            {
                logger.Fatal("Missing server channel registry!");
                throw new ArgumentException("No channel registry");
            }

            // Create connection
            logger.Info("Creating integrated server...");
            server = new IntegratedServerConnection();

            // Add components
            logger.Info("Adding components to server...");
            srv.AddComponent(new IntegratedServerComponent(server));
            if (!srv.HasConfigManager)
                srv.AddComponent(new MemoryConfigManagerComponent());

            // Call server creation
            OnCreateServer?.Invoke(srv);

            // Initialize server
            logger.Info("Initializing server...");
            srv.Init();
            locked = true;
        }

        /// <summary>
        /// Creates client factories set up for connecting to the integrated server
        /// </summary>
        /// <param name="registry">Channel registry</param>
        /// <returns>GameClientFactory instance</returns>
        public GameClientFactory CreateClientFactory(ChannelRegistry registry)
        {
            if (srv == null)
                throw new InvalidOperationException("Server not initialized");
            if (!srv.IsRunning())
                throw new InvalidOperationException("Server not running");
            GameClientFactory fac = new GameClientFactory();
            fac.WithChannelRegistry(registry);
            fac.WithIntegratedClient(() => CreateConnection(registry));
            return fac;
        }

        /// <summary>
        /// Modifies an existing client factory for connecting to the integrated server
        /// </summary>
        /// <returns>GameClientFactory instance</returns>
        public GameClientFactory AddToClientFactory(GameClientFactory fac)
        {
            if (srv == null)
                throw new InvalidOperationException("Server not initialized");
            if (!srv.IsRunning())
                throw new InvalidOperationException("Server not running");
            if (fac.ChannelRegistry == null)
                throw new ArgumentException("Client factory does not have a channel registry! Please add a client channel registry before adding the server to the factory.");
            fac.WithIntegratedClient(() => CreateConnection(fac.ChannelRegistry));
            return fac;
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void StartServer()
        {
            if (srv == null)
                throw new InvalidOperationException("Server not initialized");
            GameServer.StartServer();
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void StopServer()
        {
            if (srv == null)
                throw new InvalidOperationException("Server not initialized");
            GameServer.StopServer();
        }

        /// <summary>
        /// Called to prepare the integrated server container, called before all game servers are loaded
        /// </summary>
        protected abstract void Prepare();

        /// <summary>
        /// Called to set up the game server
        /// </summary>
        protected abstract GameServer SetupServer();
    }
}