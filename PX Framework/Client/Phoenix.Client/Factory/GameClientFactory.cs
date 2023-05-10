using Phoenix.Client.Components;
using Phoenix.Client.Factory;
using Phoenix.Client.Providers;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Registry;
using static Phoenix.Client.Providers.IClientConnectionProvider;

namespace Phoenix.Client
{
    /// <summary>
    /// Handles insecure mode servers
    /// </summary>
    /// <returns>True if the connection may proceed, false to cancel it</returns>
    public delegate bool InsecureModeHandler();

    /// <summary>
    /// Call this in case the server is in insecure mode. Should it return false, return null from the client connection provider
    /// </summary>
    /// <returns>True if the connection should be made, false otherwise</returns>
    public delegate bool InsecureModeCallback();

    /// <summary>
    /// Simple constructor delegate
    /// </summary>
    /// <returns>Connection instance</returns>
    public delegate Connection ClientConstructor();

    /// <summary>
    /// Creates the client connection
    /// </summary>
    /// <param name="insecureModeCallback">Callback to use in case the server being connected to is in insecure mode</param>
    /// <param name="connectionInfo">Output connection info (a dummy by default, highly recommended to set)</param>
    /// <returns>ClientConstructor instance or null to fail the connection</returns>
    public delegate ClientConstructor? ClientConnectionProvider(InsecureModeCallback insecureModeCallback, ref ConnectionInfo connectionInfo);

    /// <summary>
    /// Game Client Builder
    /// </summary>
    public class GameClientFactory
    {
        private List<Component> _components = new List<Component>();
        private List<IComponentPackage> _componentPackages = new List<IComponentPackage>();

        /// <summary>
        /// Controls if the factory should print a warning message in case there is no authenticator present
        /// </summary>
        public bool WarnMissingAuthenticator = true;

        /// <summary>
        /// Handles insecure mode servers
        /// </summary>
        public InsecureModeHandler InsecureModeHandler = () => true;

        /// <summary>
        /// Controls if insecure-mode servers should be allowed to connect to
        /// </summary>
        public bool AllowInsecureMode = false;

        /// <summary>
        /// Controls if insecure-mode servers should be allowed to connect to
        /// </summary>
        /// <param name="allow">True to allow insecure-mode servers, false otherwise</param>
        public GameClientFactory WithAllowInsecureMode(bool allow)
        {
            AllowInsecureMode = allow;
            InsecureModeHandler = () => true;
            return this;
        }

        /// <summary>
        /// Controls if insecure-mode servers should be allowed to connect to
        /// </summary>
        /// <param name="allow">True to allow insecure-mode servers, false otherwise</param>
        /// <param name="handler">Handler called to handle insecure-mode servers for an extra layer of confirmation</param>
        public GameClientFactory WithAllowInsecureMode(bool allow, InsecureModeHandler handler)
        {
            AllowInsecureMode = allow;
            InsecureModeHandler = handler;
            return this;
        }

        /// <summary>
        /// Client connection provider (required)
        /// </summary>
        public ClientConnectionProvider? ConnectionProvider;

        /// <summary>
        /// Sets the connection provider (<b>note: its recommended to use a connection provider library's extension methods</b>)
        /// </summary>
        /// <param name="connectionProvider">Connection provider</param>
        public GameClientFactory WithConnectionProvider(ClientConnectionProvider connectionProvider)
        {
            ConnectionProvider = connectionProvider;
            return this;
        }

        /// <summary>
        /// Packet channel registry
        /// </summary>
        public ChannelRegistry? ChannelRegistry;

        /// <summary>
        /// Defines the protocol version
        /// </summary>
        public int ProtocolVersion = -1;

        /// <summary>
        /// Defines if the client should be initialized on build
        /// </summary>
        public bool AutoInit = false;

        /// <summary>
        /// Defines if the client should connect on build
        /// </summary>
        public bool AutoConnect = false;

        /// <summary>
        /// Defines if the client should be initialized on build
        /// </summary>
        /// <param name="autoInit">True to automatically initialize the client, false otherwise</param>
        public GameClientFactory WithAutoInit(bool autoInit)
        {
            AutoInit = autoInit;
            return this;
        }

        /// <summary>
        /// Defines if the client should connect on build
        /// </summary>
        /// <param name="autoConnect">True to automatically connect on build, false otherwise</param>
        public GameClientFactory WithAutoConnect(bool autoConnect)
        {
            AutoConnect = autoConnect;
            return this;
        }

        /// <summary>
        /// Adds a protocol version (recommended)
        /// </summary>
        /// <param name="protocolVersion">Game protocol version</param>
        public GameClientFactory WithProtocolVersion(int protocolVersion)
        {
            ProtocolVersion = protocolVersion;
            return this;
        }

        /// <summary>
        /// Adds a channel registry (required)
        /// </summary>
        /// <param name="registry">Channel registry to add</param>
        public GameClientFactory WithChannelRegistry(ChannelRegistry registry)
        {
            ChannelRegistry = registry;
            return this;
        }

        /// <summary>
        /// Adds components
        /// </summary>
        /// <param name="component">Component to add</param>
        public GameClientFactory WithComponent(Component component)
        {
            _components.Add(component);
            return this;
        }

        /// <summary>
        /// Adds authenticators (highly recommended)
        /// </summary>
        /// <param name="component">Authenticator to add</param>
        public GameClientFactory WithAuthenticator(AuthenticationComponent component)
        {
            _components.Add(component);
            return this;
        }

        /// <summary>
        /// Adds component packages
        /// </summary>
        /// <param name="package">Component package to add</param>
        public GameClientFactory WithComponentPacakge(IComponentPackage packets)
        {
            _componentPackages.Add(packets);
            return this;
        }

        /// <summary>
        /// Builds the client
        /// </summary>
        /// <param name="logId">Client logger ID</param>
        /// <returns>GameClientBuildResult object</returns>
        public GameClientBuildResult Build(string logId)
        {
            // Check first
            if (ChannelRegistry == null)
                return new GameClientBuildResult() { FailureCode = GameClientBuildFailureCode.MISSING_CHANNEL_REGISTRY };
            if (ConnectionProvider == null)
                return new GameClientBuildResult() { FailureCode = GameClientBuildFailureCode.MISSING_CONNECTION_PROVIDER };
            bool secureModeFailure = false;
            ConnectionInfo info = new ConnectionInfo("integrated", 0);
            ClientConstructor? cl = null;
            try
            {
                cl = ConnectionProvider(() => {
                    if (!AllowInsecureMode)
                    {
                        secureModeFailure = true;
                        return false;
                    }
                    return InsecureModeHandler();
                }, ref info);
                if (secureModeFailure)
                    return new GameClientBuildResult() { FailureCode = GameClientBuildFailureCode.INSECURE_MODE_SERVER };
                if (cl == null)
                    return new GameClientBuildResult() { FailureCode = GameClientBuildFailureCode.CANCELLED };
            }
            catch
            {
                return new GameClientBuildResult() { FailureCode = GameClientBuildFailureCode.CONNECTION_TEST_FAILED };
            }

            // Create game client
            GameClient client = new GameClient(logId);
            client.ClientLogger.Info("Preparing to start client...");
            client.ProtocolVersion = ProtocolVersion;
            client.ChannelRegistry = ChannelRegistry;
            client.ClientLogger.Info("Adding components...");
            foreach (IComponentPackage pkg in _componentPackages)
                client.AddComponentPackage(pkg);
            foreach (Component comp in _components)
                client.AddComponent(comp);
            client.AddComponent(new GenericClientConnectionProvider(cl, info));

            // Warn if no authenticator is present
            if (WarnMissingAuthenticator && !_components.Any(t => t is AuthenticationComponent) && !_componentPackages.Any(t => t.Components.Any(t2 => t2 is AuthenticationComponent)))
            {
                client.ClientLogger.Debug("Warning! No authenticator in component path! It is highly recommended to add a authenticator else the client may freeze on connection!");
                client.ClientLogger.Debug("You can disable this warning by setting WarnMissingAuthenticator to false in the game client factory object");
            }

            // Auto-init
            if (AutoInit)
            {
                client.ClientLogger.Info("Initializing client...");
                try
                {
                    client.Init();
                }
                catch
                {
                    return new GameClientBuildResult() { FailureCode = GameClientBuildFailureCode.AUTO_INIT_FAILURE };
                }
            }

            // Auto-connect
            if (AutoConnect)
            {
                client.ClientLogger.Info("Connecting to server...");
                try
                {
                    client.Connect();
                }
                catch
                {
                    return new GameClientBuildResult() { FailureCode = GameClientBuildFailureCode.AUTO_CONNECT_FAILURE, Client = client };
                }
            }

            // Return client
            return new GameClientBuildResult() { Client = client };
        }
    }
}
