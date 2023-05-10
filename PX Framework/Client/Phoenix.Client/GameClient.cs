using Phoenix.Client.Components;
using Phoenix.Client.Components.Rules;
using Phoenix.Client.Events;
using Phoenix.Client.Providers;
using Phoenix.Common;
using Phoenix.Common.Events;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Exceptions;
using Phoenix.Common.Networking.Impl;
using Phoenix.Common.Networking.Registry;
using Phoenix.Common.Services;
using System.Diagnostics;
using System.Net.Sockets;

namespace Phoenix.Client
{
    /// <summary>
    /// Phoenix Game Client
    /// </summary>
    public class GameClient
    {
        private static List<GameClient> engineLinkedGameClients = new List<GameClient>();

        /// <summary>
        /// Ticks all clients presently running
        /// </summary>
        public static void GlobalTick()
        {
            GameClient[] clients;
            while (true)
            {
                try
                {
                    clients = engineLinkedGameClients.ToArray();
                    break;
                }
                catch { }
            }
            foreach (GameClient client in clients)
            {
                if (client == null)
                {
                    engineLinkedGameClients.Remove(client);
                    continue;
                }
                if (!Debugger.IsAttached)
                {
                    try
                    {
                        client.ClientTick();
                    }
                    catch { }
                }
                else
                    client.ClientTick();
            }
        }

        private Logger Logger;
        private EventBus EventBus = new EventBus();
        private ServiceManager Manager = new ServiceManager();
        private List<Component> Components = new List<Component>();
        private ChannelRegistry Registry;
        private Connection Client;
        private bool RegistryLocked;
        private bool Loaded;
        private bool Connected;

        private int _protocol = -1;
        private string _gameVersion = "";
        private string? _gameID;

        /// <summary>
        /// Basic tick event handler
        /// </summary>
        public delegate void TickEventHandler();

        /// <summary>
        /// Called on each client tick
        /// </summary>
        public event TickEventHandler? OnTick;

        /// <summary>
        /// Called at the end of each client tick
        /// </summary>
        public event TickEventHandler? OnPostTick;

        /// <summary>
        /// Checks if the client is initialized
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                return RegistryLocked;
            }
        }

        private DisconnectParams? _overrideReason;

        /// <summary>
        /// Disconnect reason parameters
        /// </summary>
        public DisconnectParams? DisconnectReason
        {
            get
            {
                if (_overrideReason != null)
                    return _overrideReason;
                if (Client == null)
                    return null;
                return Client.DisconnectReason;
            }
            private set
            {
                _overrideReason = value;
            }
        }

        /// <summary>
        /// Client start event handler
        /// </summary>
        /// <param name="connection">Client connection</param>
        public delegate void ClientStartHandler(Connection connection);

        /// <summary>
        /// Client start failure event handler
        /// </summary>
        /// <param name="connection">Client connection</param>
        /// <param name="failure">Failure type</param>
        public delegate void ClientStartFailureHandler(Connection connection, ClientStartFailureType failure);

        /// <summary>
        /// Client late handshake event handler
        /// </summary>
        /// <param name="connection">Client connection</param>
        public delegate void ClientLateHandshakeHandler(Connection connection, ConnectionEventArgs args);

        /// <summary>
        /// Client start success event handler
        /// </summary>
        /// <param name="connection">Client connection</param>
        public delegate void ClientConnectedHandler(Connection connection);

        /// <summary>
        /// Client disconnect event handler
        /// </summary>
        /// <param name="connection">Client connection</param>
        /// <param name="reason">Disconnect reason</param>
        /// <param name="args">Disconnect reason arguments</param>
        public delegate void ClientDisconnectHandler(Connection connection, string reason, string[] args);

        /// <summary>
        /// Client startup event - Called early in the startup process
        /// </summary>
        public event ClientStartHandler? OnStart;

        /// <summary>
        /// Client startup failure event - Called when startup fails
        /// </summary>
        public event ClientStartFailureHandler? OnStartFailure;

        /// <summary>
        /// Client late handshake event - Called when the connection has been established and allows for non-packet traffic, called just before the packet handlers are started
        /// </summary>
        public event ClientLateHandshakeHandler? OnLateHandshake;

        /// <summary>
        /// Client startup success event
        /// </summary>
        public event ClientConnectedHandler? OnConnected;

        /// <summary>
        /// Client disconnect event
        /// </summary>
        public event ClientDisconnectHandler? OnDisconnected;

        /// <summary>
        /// Instantiates a new game client
        /// </summary>
        /// <param name="logId">Logger ID</param>
        public GameClient(string logId)
        {
            Logger = Logger.GetLogger(logId);
        }

        /// <summary>
        /// The channel registry of this client
        /// </summary>
        public ChannelRegistry ChannelRegistry
        {
            get
            {
                return Registry;
            }
            set
            {
                if (RegistryLocked)
                    throw new InvalidOperationException("Registry locked");
                Registry = value;
            }
        }

        /// <summary>
        /// Retrieves the client connection
        /// </summary>
        public Connection ClientConnection
        {
            get
            {
                if (Client == null)
                    throw new InvalidOperationException("Client not initialized");
                return Client;
            }
        }

        /// <summary>
        /// Defines the protocol version
        /// </summary>
        public int ProtocolVersion
        {
            get
            {
                return _protocol;
            }
            set
            {
                if (RegistryLocked)
                    throw new InvalidOperationException("Registry locked");
                _protocol = value;
            }
        }

        /// <summary>
        /// Retrieves a component instance by type
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Component instance</returns>
        public T GetComponent<T>() where T : Component
        {
            if (!Loaded)
                throw new InvalidOperationException("Client not initialized");
            foreach (Component comp in Components)
            {
                if (comp is T)
                    return (T)comp;
            }

            throw new ArgumentException("Unrecognized component");
        }

        /// <summary>
        /// Retrieves a component instance by type
        /// </summary>
        /// <param name="type">Component type</param>
        /// <returns>Component instance</returns>
        public Component GetComponent(Type type)
        {
            if (!Loaded)
                throw new InvalidOperationException("Client not initialized");
            foreach (Component comp in Components)
            {
                if (type.IsAssignableFrom(comp.GetType()))
                    return comp;
            }

            throw new ArgumentException("Unrecognized component");
        }

        /// <summary>
        /// Retrieves a component instance by ID
        /// </summary>
        /// <param name="id">Component ID</param>
        /// <returns>Component instance</returns>
        public Component GetComponent(string id)
        {
            if (!Loaded)
                throw new InvalidOperationException("Client not initialized");
            Component? comp = GetComponentByID(id, null);
            if (comp == null)
                throw new ArgumentException("Unrecognized component");
            return comp;
        }

        /// <summary>
        /// Checks if a component is loaded
        /// </summary>
        /// <param name="id">Component ID</param>
        /// <returns>True if loaded, false otherwise</returns>
        public bool IsComponentLoaded(string id)
        {
            if (!Loaded)
                throw new InvalidOperationException("Client not initialized");
            return GetComponentByID(id, null) != null;
        }

        /// <summary>
        /// Retrieves all components
        /// </summary>
        /// <returns>Array of Component instances</returns>
        public Component[] GetComponents()
        {
            if (!Loaded)
                throw new InvalidOperationException("Client not initialized");
            return Components.ToArray();
        }

        /// <summary>
        /// Client service manager
        /// </summary>
        public ServiceManager ServiceManager
        {
            get
            {
                return Manager;
            }
        }

        /// <summary>
        /// The client event bus
        /// </summary>
        public EventBus ClientEventBus
        {
            get
            {
                return EventBus;
            }
        }

        /// <summary>
        /// Retrieves the client logger
        /// </summary>
        public Logger ClientLogger
        {
            get
            {
                return Logger;
            }
        }

        /// <summary>
        /// Adds a component to the client
        /// </summary>
        /// <param name="component">Component to add</param>
        public void AddComponent(Component component)
        {
            if (RegistryLocked)
                throw new InvalidOperationException("Registry locked");
            if (Components.Any(t => t.GetType().IsAssignableFrom(component.GetType())))
            {
                Logger.Fatal("Attempted to register " + component.ID + " twice!");
                throw new ArgumentException("Component already registered");
            }
            Logger.Info("Registering component: " + component.ID);
            component.InitLoadRules();
            if (component is ClientComponent)
                ((ClientComponent)component).PassGameClient(this);
            Components.Add(component);
        }

        /// <summary>
        /// Adds a component package to the client
        /// </summary>
        /// <param name="package">Component package to add</param>
        public void AddComponentPackage(IComponentPackage package)
        {
            if (RegistryLocked)
                throw new InvalidOperationException("Registry locked");
            Logger.Info("Registering components from package: " + package.ID);
            foreach (Component component in package.Components)
            {
                Logger.GlobalMessagePrefix += "  ";
                if (Components.Any(t => t.GetType().IsAssignableFrom(component.GetType())))
                {
                    Logger.Fatal("Attempted to register " + component.ID + " twice!");
                    Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
                    throw new ArgumentException("Component already registered");
                }
                Logger.Info("Registering component: " + component.ID);
                component.InitLoadRules();
                if (component is ClientComponent)
                    ((ClientComponent)component).PassGameClient(this);
                Components.Add(component);
                Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
            }
            Logger.Info("Components registered from package: " + package.ID);
        }

        /// <summary>
        /// Initializes the client
        /// </summary>
        public void Init()
        {
            if (Loaded)
                throw new InvalidOperationException("Already initialized");
            if (RegistryLocked)
                throw new InvalidOperationException("Already initializing");
            _overrideReason = null;

            // Packet registry
            if (Registry == null)
                throw new ArgumentException("No packet registry");

            // Game ID and protocol version
            if (_protocol == -1)
                Logger.Warn("Protocol version not defined! Please assign ProtocolVersion before initializing the client, using -1 as version!");
            if (Game.GameID == null)
            {
                Logger.Warn("Game ID not defined! Please add a Game information implementation!");
                _gameID = "generic";
                _gameVersion = "unknown";
            }
            else
            {
                _gameID = Game.GameID;
                _gameVersion = Game.Version;
                Logger.Info("");
                string msg = "   " + Game.Title + " Client, Version " + Game.Version + "/" + _protocol + "/" + Connections.PhoenixProtocolVersion + "   ";
                string line = "";
                for (int i = 0; i < msg.Length; i++)
                    line += "-";
                Logger.Info(line);
                Logger.Info("");
                Logger.Info(msg);
                Logger.Info("");
                Logger.Info(" Game ID: " + Game.GameID);
                Logger.Info(" Version: " + Game.Version);
                Logger.Info(" Phoenix Protocol: " + Connections.PhoenixProtocolVersion);
                Logger.Info(" Game Protocol: " + _protocol);
                Logger.Info("");
                Logger.Info(line);
                Logger.Info("");
            }

            // Lock
            Logger.Trace("Locking registry...");
            RegistryLocked = true;

            // Load client components
            Logger.Info("Preparing to load components...");
            List<Component> loadingComponents = new List<Component>();
            Dictionary<string, List<string>> deps = new Dictionary<string, List<string>>();
            List<Component> loadOrder = new List<Component>();

            // Load-after rules
            foreach (Component comp in Components)
            {
                foreach (ComponentLoadRule rule in comp.LoadRules.Where(t => t.Type == ComponentLoadRuleType.LOADAFTER))
                {
                    if (!deps.ContainsKey(rule.Target))
                        deps[rule.Target] = new List<string>();
                    deps[rule.Target].Add(comp.ID);
                }
            }

            // Load components
            foreach (Component comp in Components)
            {
                LoadComponent(comp, loadOrder, loadingComponents, deps);
            }
            Loaded = true;
            Components = loadOrder;
        }

        private int ticksRun = 0;
        private int tps;

        /// <summary>
        /// Retrieves the current TPS (ticks per second)
        /// </summary>
        public int TPS
        {
            get
            {
                return tps;
            }
        }

        /// <summary>
        /// Ticks the client
        /// </summary>
        public void ClientTick()
        {
            if (!Connected)
                throw new InvalidOperationException("Client is not connected");

            // Tick client

            // Tick the world
            long lastTickStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Tick components
            foreach (Component comp in Components)
                comp.Tick();
            OnTick?.Invoke();
            long tickTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastTickStart;
            if (!IsConnected())
                return;
            ticksRun++;
            if (tickTime > 500)
            {
                // This took too long, throw a warning
                Logger.Warn("Client tick took too long to complete! Tick took " + tickTime + "ms! Cannot fast-forward client ticks unlike server ticks, beware of lag!");
            }

            // Run post-tick
            OnPostTick?.Invoke();
        }

        /// <summary>
        /// Checks if the client is connected
        /// </summary>
        public bool IsConnected()
        {
            return Connected;
        }

        private void LoadComponent(Component component, List<Component> outp, List<Component> loadingComponents, Dictionary<string, List<string>> deps)
        {
            if (loadingComponents.Contains(component))
                return;
            loadingComponents.Add(component);

            // Conflicts
            foreach (ComponentLoadRule rule in component.LoadRules.Where(t => t.Type == ComponentLoadRuleType.CONFLICT))
            {
                if (GetComponentByID(rule.Target, component) != null)
                {
                    // Fatal error
                    Logger.GlobalMessagePrefix += "  ";
                    Logger.Fatal("Conflict! Component " + component.ID + " conflicts with " + rule.Target + "! Unable to continue!");
                    Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
                    throw new ArgumentException("Component conflict: " + component.ID + " conflicts with " + rule.Target);
                }
            }

            // Dependencies
            foreach (ComponentLoadRule rule in component.LoadRules.Where(t => t.Type == ComponentLoadRuleType.DEPENDENCY))
            {
                Component? comp = GetComponentByID(rule.Target, component);
                if (comp == null)
                {
                    // Fatal error
                    Logger.GlobalMessagePrefix += "  ";
                    Logger.Fatal("Missing dependencies!\nComponent " + component.ID + " requires component " + rule.Target + " to be registered.");
                    Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
                    throw new ArgumentException("Missing dependency: " + component.ID + " requires " + rule.Target);
                }

                LoadComponent(comp, outp, loadingComponents, deps);
            }

            // Optional dependencies
            foreach (ComponentLoadRule rule in component.LoadRules.Where(t => t.Type == ComponentLoadRuleType.OPTDEPEND))
            {
                Component? comp = GetComponentByID(rule.Target, component);
                if (comp == null)
                    continue;
                LoadComponent(comp, outp, loadingComponents, deps);
            }

            // Load-before
            foreach (string dep in deps.GetValueOrDefault(component.ID, new List<string>()))
            {
                Component? comp = GetComponentByID(dep, component);
                if (comp == null)
                    continue;
                LoadComponent(comp, outp, loadingComponents, deps);
            }

            // Load component
            outp.Add(component);
            Logger.GlobalMessagePrefix += "  ";
            Logger.Info("Pre-initializing component: " + component.ID);
            component.PreInit();
            EventBus.AttachAll(component);
            Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
        }

        private bool handledHandshakeFailure = false;

        /// <summary>
        /// Starts the client connection
        /// </summary>
        public void Connect()
        {
            if (!Loaded)
                throw new InvalidOperationException("Client not initialized");
            if (Connected)
                throw new InvalidOperationException("Client is already connected");
            clientConnectionAuthFailure = false;
            handledHandshakeFailure = false;
            _overrideReason = null;

            // Prepare client
            Logger.Info("Preparing to connect to server...");

            // Find Provider
            IClientConnectionProvider? provider = null;
            foreach (Component comp in Components)
                if (comp is IClientConnectionProvider)
                {
                    Logger.Trace("Using client provider: " + comp.GetType().FullName);
                    provider = (IClientConnectionProvider)comp;
                    break;
                }
            if (provider == null)
            {
                Logger.Fatal("Cannot start the client! Missing a client connection component!");
                throw new ArgumentException("No client connection provider component");
            }
            EventBus.Dispatch(new ClientStartupPrepareEvent(this));
            Logger.Trace("Creating client connection...");
            Connection conn = provider.Provide();
            if (conn.IsConnected())
            {
                Logger.Error("Connection already open!");
                DisconnectReason = new DisconnectParams("connect.error.alreadyopen", new string[0]);
                OnStartFailure?.Invoke(conn, ClientStartFailureType.CONNECTION_ALREADY_OPEN);
                throw new InvalidOperationException("Connection already open");
            }
            Client = conn;
            conn.AddObject(this);

            // Events
            Logger.Trace("Attaching events...");
            ConnectionEventHandler connectedHandler = (conn, args) =>
            {
                // (Late) program handshake
                Logger.Debug("Late handshake connection: " + conn);
                OnLateHandshake?.Invoke(conn, args);
            };
            CustomHandshakeProvider customHandshakeHandler = (conn, args) =>
            {
                // Program handshake
                if (args.HasFailed())
                {
                    // Uhhhh problem
                    Logger.Error("Unexpected handshake traffic!");
                    handledHandshakeFailure = true;
                    DisconnectReason = new DisconnectParams("connect.error.connectfailure.unexpectedtraffic", new string[0]);
                    OnStartFailure?.Invoke(conn, ClientStartFailureType.HANDSHAKE_FAILURE_UNEXPECTED_TRAFFIC);
                    EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.HANDSHAKE_FAILURE_UNEXPECTED_TRAFFIC, this));
                    return;
                }

                //
                // Perform Phoenix handshake
                //

                // Send game ID
                Logger.Trace("Performing Phoenix Game Handshake on connection: " + conn);
                Logger.Trace("Sending game ID: " + _gameID + ", protocol version " + _protocol + ", game version " + _gameVersion + " to " + conn);
                args.ClientOutput.WriteString(_gameID);

                // Send protocol
                args.ClientOutput.WriteInt(_protocol);
                args.ClientOutput.WriteString(_gameVersion);

                // Send connection details
                IClientConnectionProvider.ConnectionInfo info = provider.ProvideInfo();
                Logger.Trace("Sending connection details for " + conn + ": [" + info.ServerAddress + "]:" + info.Port);
                args.ClientOutput.WriteString(info.ServerAddress);
                args.ClientOutput.WriteInt(info.Port);

                // Read ID and protocol
                string rGID = args.ClientInput.ReadString();
                int rProtocol = args.ClientInput.ReadInt();
                string rVer = args.ClientInput.ReadString();
                Logger.Trace("Received game ID: " + rGID + ", protocol version " + rProtocol + ", game version " + rVer);
                Logger.Trace("Verifying handshake...");
                if (_gameID != rGID)
                {
                    // Fail
                    Logger.Error("Game ID mismatch!");
                    args.FailHandshake();
                    handledHandshakeFailure = true;
                    DisconnectReason = new DisconnectParams("connect.error.connectfailure.gamemismatch", new string[] { rGID, _gameID });
                    OnStartFailure?.Invoke(conn, ClientStartFailureType.HANDSHAKE_FAILURE_GAME_MISMATCH);
                    EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.HANDSHAKE_FAILURE_GAME_MISMATCH, this));
                    return;
                }
                else if (_protocol != rProtocol)
                {
                    // Fail
                    Logger.Error("Game version mismatch!");
                    args.FailHandshake();
                    handledHandshakeFailure = true;
                    DisconnectReason = new DisconnectParams("connect.error.connectfailure.versionmismatch", new string[] { rVer, _gameVersion });
                    OnStartFailure?.Invoke(conn, ClientStartFailureType.HANDSHAKE_FAILURE_VERSION_MISMATCH);
                    EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.HANDSHAKE_FAILURE_VERSION_MISMATCH, this));
                    return;
                }
                Logger.Trace("Handshake success!");
            };
            ConnectionSuccessEventHandler connectionSuccessHandler = (conn) =>
            {
                // Connection established
                Connected = true;
                OnConnected?.Invoke(conn);
                EventBus.Dispatch(new ClientConnectedEvent(this));
                engineLinkedGameClients.Add(this);
            };
            ConnectionDisconnectEventHandler clientDisconnectHandler = null;
            clientDisconnectHandler = (conn, reason, args) =>
            {
                // Disconnected
                Logger.Info("Game client connection has been terminated!");
                EventBus.Dispatch(new ClientDisconnectedEvent(this, new ClientDisconnectedEventArgs(reason, args)));
                OnDisconnected?.Invoke(conn, reason, args);
                engineLinkedGameClients.Remove(this);
                Connected = false;

                // De-init components
                Logger.Info("De-initializing components...");
                foreach (Component comp in Components)
                {
                    Logger.GlobalMessagePrefix += "  ";
                    Logger.Info("De-initializing " + comp.ID);
                    comp.DeInit();
                    Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
                }

                // Stop client
                Logger.Info("Cleaning up...");
                conn.Disconnected -= clientDisconnectHandler;
                conn.CustomHandshakes -= customHandshakeHandler;
                conn.ConnectionSuccess -= connectionSuccessHandler;
                conn.Connected -= connectedHandler;
                Logger.Info("Client disconnected successfully!");
                EventBus.Dispatch(new ClientDisconnectCompleteEvent(this, new ClientDisconnectedEventArgs(reason, args)));
            };
            conn.CustomHandshakes += customHandshakeHandler;
            conn.ConnectionSuccess += connectionSuccessHandler;
            conn.Connected += connectedHandler;

            // Initialize components
            Logger.Info("Initializing client components...");
            foreach (Component comp in Components)
            {
                Logger.GlobalMessagePrefix += "  ";
                Logger.Info("Initializing " + comp.ID);
                comp.Init();
                Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
            }

            // Attempt to connect
            Logger.Info("Attempting server connection...");

            // Call events
            OnStart?.Invoke(conn);
            EventBus.Dispatch(new ClientStartupEvent(this));

            // Attempt connection
            try
            {
                Logger.Trace("Starting client connection...");
                provider.StartGameClient();
            }
            catch (Exception e)
            {
                // Handle specific exception
                if (clientConnectionAuthFailure)
                {
                    Logger.Error("Authentication failure!");

                    // De-init components
                    Logger.Info("De-initializing components...");
                    foreach (Component comp in Components)
                    {
                        Logger.GlobalMessagePrefix += "  ";
                        Logger.Info("De-initializing " + comp.ID);
                        comp.DeInit();
                        Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
                    }
                    Logger.Info("Connection ended.");

                    if (DisconnectReason == null || DisconnectReason.Reason == "disconnect.generic")
                        DisconnectReason = new DisconnectParams("disconnect.loginfailure.authfailure", new string[0]);
                    OnStartFailure?.Invoke(conn, ClientStartFailureType.AUTHENTICATION_FAILURE);
                    EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.AUTHENTICATION_FAILURE, this));
                    throw;
                }

                // Log error
                Logger.Error("Connection failure!", e);

                // De-init components
                Logger.Info("De-initializing components...");
                foreach (Component comp in Components)
                {
                    Logger.GlobalMessagePrefix += "  ";
                    Logger.Info("De-initializing " + comp.ID);
                    comp.DeInit();
                    Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
                }
                Logger.Info("Connection ended.");

                if (e is SocketException)
                {
                    OnStartFailure?.Invoke(conn, ClientStartFailureType.CONNECT_FAILED);
                    DisconnectReason = new DisconnectParams("connect.error.connectfailure.unreachable", new string[0]);
                }
                else if (e is PhoenixConnectException)
                {
                    switch (((PhoenixConnectException)e).ErrorType)
                    {
                        case ErrorType.NONPHOENIX:
                            OnStartFailure?.Invoke(conn, ClientStartFailureType.HANDSHAKE_FAILURE_NONPHOENIX);
                            DisconnectReason = new DisconnectParams("connect.error.connectfailure.nonphoenix", new string[0]);
                            EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.HANDSHAKE_FAILURE_NONPHOENIX, this));
                            break;
                        case ErrorType.INVALID_CERTIFICATE:
                            OnStartFailure?.Invoke(conn, ClientStartFailureType.HANDSHAKE_FAILURE_INVALID_CERTIFICATE);
                            DisconnectReason = new DisconnectParams("connect.error.connectfailure.invalidcertificate", new string[0]);
                            EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.HANDSHAKE_FAILURE_INVALID_CERTIFICATE, this));
                            break;
                        case ErrorType.ENCRYPTION_KEY_REJECTED:
                            OnStartFailure?.Invoke(conn, ClientStartFailureType.HANDSHAKE_FAILURE_ENCRYPTION_FAILURE);
                            DisconnectReason = new DisconnectParams("connect.error.connectfailure.encrypterror", new string[0]);
                            EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.HANDSHAKE_FAILURE_ENCRYPTION_FAILURE, this));
                            break;
                        case ErrorType.ENCRYPTION_FAILURE:
                            OnStartFailure?.Invoke(conn, ClientStartFailureType.HANDSHAKE_FAILURE_ENCRYPTION_FAILURE);
                            DisconnectReason = new DisconnectParams("connect.error.connectfailure.encrypterror", new string[0]);
                            EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.HANDSHAKE_FAILURE_ENCRYPTION_FAILURE, this));
                            break;
                        case ErrorType.PROGRAM_HANDSHAKE_FAILURE:
                            if (!handledHandshakeFailure)
                            {
                                OnStartFailure?.Invoke(conn, ClientStartFailureType.HANDSHAKE_FAILURE_UNEXPECTED_TRAFFIC);
                                DisconnectReason = new DisconnectParams("connect.error.connectfailure.unexpectedtraffic", new string[0]);
                                EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.HANDSHAKE_FAILURE_UNEXPECTED_TRAFFIC, this));
                            }
                            break;
                    }
                }
                else if (e is IOException)
                {
                    OnStartFailure?.Invoke(conn, ClientStartFailureType.CONNECT_FAILED);
                    DisconnectReason = new DisconnectParams("connect.error.connectfailure.unreachable", new string[0]);
                    EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.CONNECT_FAILED, this));
                }
                else
                {
                    OnStartFailure?.Invoke(conn, ClientStartFailureType.UNKOWN_ERROR);
                    DisconnectReason = new DisconnectParams("connect.error.connectfailure.inernalerror", new string[0]);
                    EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.UNKOWN_ERROR, this));
                }
                conn.CustomHandshakes -= customHandshakeHandler;
                conn.ConnectionSuccess -= connectionSuccessHandler;
                conn.Connected -= connectedHandler;

                throw;
            }
            if (!conn.IsConnected())
            {
                conn.CustomHandshakes -= customHandshakeHandler;
                conn.ConnectionSuccess -= connectionSuccessHandler;
                conn.Connected -= connectedHandler;

                // Authentication failure
                if (clientConnectionAuthFailure)
                {
                    Logger.Error("Authentication failure!");

                    // De-init components
                    Logger.Info("De-initializing components...");
                    foreach (Component comp in Components)
                    {
                        Logger.GlobalMessagePrefix += "  ";
                        Logger.Info("De-initializing " + comp.ID);
                        comp.DeInit();
                        Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
                    }
                    Logger.Info("Connection ended.");

                    if (DisconnectReason == null || DisconnectReason.Reason == "disconnect.generic")
                        DisconnectReason = new DisconnectParams("disconnect.loginfailure.authfailure", new string[0]);
                    OnStartFailure?.Invoke(conn, ClientStartFailureType.AUTHENTICATION_FAILURE);
                    EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.AUTHENTICATION_FAILURE, this));
                    throw new IOException("Authentication failure");
                }

                // Log error
                Logger.Error("Server closed the connection too early!");

                // De-init components
                Logger.Info("De-initializing components...");
                foreach (Component comp in Components)
                {
                    Logger.GlobalMessagePrefix += "  ";
                    Logger.Info("De-initializing " + comp.ID);
                    comp.DeInit();
                    Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
                }
                Logger.Info("Connection ended.");

                if (DisconnectReason == null || DisconnectReason.Reason == "disconnect.generic")
                    DisconnectReason = new DisconnectParams("connect.error.connectfailure.endedearly", new string[0]);
                OnStartFailure?.Invoke(conn, ClientStartFailureType.ENDED_TOO_EARLY);
                EventBus.Dispatch(new ClientStartupFailureEvent(ClientStartFailureType.ENDED_TOO_EARLY, this));
                throw new IOException("Server terminated client connection before competion");
            }
            Logger.Trace("Successfully connected to " + conn.GetRemoteAddress());
            conn.Disconnected += clientDisconnectHandler;

            // Post-initialize components
            Logger.Info("Post-initializing components...");
            foreach (Component comp in Components)
            {
                Logger.GlobalMessagePrefix += "  ";
                Logger.Info("Post-initializing " + comp.ID);
                comp.PostInit();
                Logger.GlobalMessagePrefix = Logger.GlobalMessagePrefix.Substring(2);
            }
            Logger.Info("Connected successfully to server");

            // Start TPS counter
            Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
            {
                int warnState = 0;
                bool hasTicked = false;
                while (IsConnected())
                {
                    if (warnState <= 5)
                    {
                        if (warnState == 5 && !hasTicked)
                        {
                            Logger.Warn("");
                            Logger.Warn("WARNING! The client has not been ticked yet! This is most likely due to the tick function not being called.");
                            Logger.Warn("Please add a GameClient.ClientTick() call to your game engine's update event to make sure the client is ticked.");
                            Logger.Warn("");
                            Logger.Warn("It is HIGLY recommended to use the engine's  update event as that will synchronize the client ticker to the engine's thread.");
                            Logger.Warn("Many components and modules will use the task manager or other tick events to call UI code on the UI thread as else most engines will fail to update the UI or 3D environment.");
                            Logger.Warn("Please read the documentation on your engine of choice for how to run code on update.");
                            Logger.Warn("");
                        }
                        warnState++;
                    }
                    tps = ticksRun;
                    if (tps != 0)
                    {
                        hasTicked = true;
                        warnState = 6;
                    }
                    ticksRun = 0;
                    Thread.Sleep(1000);
                }
            });
        }

        /// <summary>
        /// Disconnects the client
        /// </summary>
        public void Disconnect()
        {
            if (!Connected)
                throw new InvalidOperationException("Client is not connected");
            engineLinkedGameClients.Remove(this);

            Logger.Info("Stopping client...");
            IClientConnectionProvider? provider = null;
            foreach (Component comp in Components)
                if (comp is IClientConnectionProvider)
                {
                    provider = (IClientConnectionProvider)comp;
                    break;
                }
            if (provider == null)
            {
                Logger.Fatal("Cannot start the client! Missing a client connection component!");
                throw new ArgumentException("No client connection provider component");
            }
            provider.StopGameClient();
        }

        private Component? GetComponentByID(string id, Component? source)
        {
            foreach (Component comp in Components)
            {
                if (comp.ID == id && (source == null || comp != source))
                    return comp;
            }
            foreach (Component comp in Components)
            {
                if (comp.Aliases.Any(t => t == id) && (source == null || comp != source))
                    return comp;
            }
            return null;
        }

        private bool clientConnectionAuthFailure = false;
        internal void FailAuth()
        {
            clientConnectionAuthFailure = true;
        }
    }
}