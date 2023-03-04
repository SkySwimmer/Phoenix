using System.Text;
using Phoenix.Common;
using Phoenix.Common.Events;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Registry;
using Phoenix.Common.Services;
using Phoenix.Server.Components;
using Phoenix.Server.Components.Rules;
using Phoenix.Server.Configuration;
using Phoenix.Server.Events;
using Phoenix.Server.ServerImplementations;

namespace Phoenix.Server
{
    /// <summary>
    /// Phoenix Game Server
    /// </summary>
    public class GameServer
    {
        private Logger Logger;
        private EventBus EventBus = new EventBus();
        private ServiceManager Manager = new ServiceManager();
        private List<Component> Components = new List<Component>();
        private ChannelRegistry Registry;
        private ServerConnection Server;
        private bool RegistryLocked;
        private bool Loaded;
        private bool Running;

        /// <summary>
        /// Server configuration overrides (usually commandline-assigned configuaration options)
        /// </summary>
        public Dictionary<string, string> ConfigurationOverrides = new Dictionary<string, string>();

        private IConfigurationManager _configManager;
        private string _dataPrefix = "";
        private string _ip = "0.0.0.0";
        private int _port = 16719;
        private int _protocol = -1;
        private string _gameID;
        private bool _retrievingConfig = false;

        /// <summary>
        /// Basic tick event handler
        /// </summary>
        public delegate void TickEventHandler();

        /// <summary>
        /// Called on each server tick
        /// </summary>
        public event TickEventHandler? OnTick;

        /// <summary>
        /// Called at the end of each server tick
        /// </summary>
        public event TickEventHandler? OnPostTick;

        /// <summary>
        /// Checks if a configuration manager is present
        /// </summary>
        public bool HasConfigManager
        {
            get
            {
                if (_configManager != null)
                    return true;

                // Find manager component
                foreach (Component comp in Components)
                {
                    if (comp is IConfigurationManager)
                    {
                        _configManager = (IConfigurationManager)comp;
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Retrieves configurations by name
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <returns>AbstractConfigurationSegment instance</returns>
        public AbstractConfigurationSegment GetConfiguration(string name)
        {
            if (!RegistryLocked && _configManager == null)
                throw new InvalidOperationException("Server not initialized");
            while (_retrievingConfig)
                Thread.Sleep(100);
            _retrievingConfig = true;
            if (_configManager == null)
            {
                // Find manager component
                foreach (Component comp in Components)
                {
                    if (comp is IConfigurationManager)
                    {
                        _configManager = (IConfigurationManager)comp;
                        break;
                    }
                }
                if (_configManager == null)
                    throw new ArgumentException("No configuration manager implementation");
            }
            AbstractConfigurationSegment seg = _configManager.GetConfiguration(name);
            _retrievingConfig = false;
            return seg;
        }

        /// <summary>
        /// Server data prefix
        /// </summary>
        public string DataPrefix
        {
            get
            {
                return _dataPrefix;
            }
            set
            {
                if (Running)
                    throw new InvalidOperationException("Server is running and cannot have its configuration changed");
                _dataPrefix = value;
            }
        }

        /// <summary>
        /// Server port
        /// </summary>
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (Running)
                    throw new InvalidOperationException("Server is running and cannot have its configuration changed");
                _port = value;
            }
        }

        /// <summary>
        /// Server Address
        /// </summary>
        public string Address
        {
            get
            {
                return _ip;
            }
            set
            {
                if (Running)
                    throw new InvalidOperationException("Server is running and cannot have its configuration changed");
                _ip = value;
            }
        }

        /// <summary>
        /// The channel registry of this server
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
        /// Retrieves the server connection
        /// </summary>
        public ServerConnection ServerConnection
        {
            get
            {
                if (Server == null)
                    throw new InvalidOperationException("Server not initialized");
                return Server;
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
                throw new InvalidOperationException("Server not initialized");
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
                throw new InvalidOperationException("Server not initialized");
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
                throw new InvalidOperationException("Server not initialized");
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
                throw new InvalidOperationException("Server not initialized");
            return GetComponentByID(id, null) != null;
        }

        /// <summary>
        /// Retrieves all components
        /// </summary>
        /// <returns>Array of Component instances</returns>
        public Component[] GetComponents()
        {
            if (!Loaded)
                throw new InvalidOperationException("Server not initialized");
            return Components.ToArray();
        }

        /// <summary>
        /// Server service manager
        /// </summary>
        public ServiceManager ServiceManager
        {
            get
            {
                return Manager;
            }
        }

        /// <summary>
        /// The server event bus
        /// </summary>
        public EventBus ServerEventBus
        {
            get
            {
                return EventBus;
            }
        }

        /// <summary>
        /// Retrieves the server logger
        /// </summary>
        public Logger ServerLogger
        {
            get
            {
                return Logger;
            }
        }

        /// <summary>
        /// Instantiates a new game server
        /// </summary>
        /// <param name="logId">Logger ID</param>
        public GameServer(string logId)
        {
            Logger = Logger.GetLogger(logId);
        }

        /// <summary>
        /// Adds a component to the server
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
            if (component is ServerComponent)
                ((ServerComponent)component).PassGameServer(this);
            Components.Add(component);
        }

        /// <summary>
        /// Adds a component package to the server
        /// </summary>
        /// <param name="package">Component package to add</param>
        public void AddComponentPackage(IComponentPackage package)
        {
            if (RegistryLocked)
                throw new InvalidOperationException("Registry locked");
            Logger.Info("Registering components from package: " + package.ID);
            foreach (Component component in package.Components)
            {
                Phoenix.Common.Logging.Logger.GlobalMessagePrefix += "  ";
                if (Components.Any(t => t.GetType().IsAssignableFrom(component.GetType())))
                {
                    Logger.Fatal("Attempted to register " + component.ID + " twice!");
                    Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);
                    throw new ArgumentException("Component already registered");
                }
                Logger.Info("Registering component: " + component.ID);
                component.InitLoadRules();
                if (component is ServerComponent)
                    ((ServerComponent)component).PassGameServer(this);
                Components.Add(component);
                Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);
            }
            Logger.Info("Components registered from package: " + package.ID);
        }

        /// <summary>
        /// Initializes the server
        /// </summary>
        public void Init()
        {
            if (Loaded)
                throw new InvalidOperationException("Already initialized");
            if (RegistryLocked)
                throw new InvalidOperationException("Already initializing");

            // Packet registry
            if (Registry == null)
                throw new ArgumentException("No packet registry");

            // Game ID and protocol version
            if (_protocol == -1)
                Logger.Warn("Protocol version not defined! Please assign ProtocolVersion before initializing the server, using -1 as version!");
            if (Game.GameID == null)
            {
                Logger.Warn("Game ID not defined! Please add a Game information implementation!");
                _gameID = "generic";
            }
            else
            {
                _gameID = Game.GameID;
                Logger.Info("");
                string msg = "   " + Game.Title + " Server, Version " + Game.Version + "/" + _protocol + "/" + Connections.PhoenixProtocolVersion + "   ";
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

            // Load port and IP from config
            Logger.Trace("Loading configuration manager...");
            if (_configManager == null)
            {
                // Find manager component
                foreach (Component comp in Components)
                {
                    if (comp is IConfigurationManager)
                    {
                        _configManager = (IConfigurationManager)comp;
                        Logger.Trace("Configuration manager loaded: " + _configManager.GetType().FullName);
                        break;
                    }
                }
            }
            if (_configManager == null)
                Logger.Trace("Found no configuration manager on the component path");

            // Lock
            Logger.Trace("Locking registry...");
            RegistryLocked = true;

            // Load server components
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

        /// <summary>
        /// Starts the server
        /// </summary>
        public void StartServer()
        {
            if (!Loaded)
                throw new InvalidOperationException("Server not initialized");
            if (Running)
                throw new InvalidOperationException("Server is already running");

            // Prepare server
            Logger.Info("Preparing to start server...");
            EventBus.Dispatch(new PrepareServerEvent(this));

            // Find Provider
            Logger.Trace("Finding server provider...");
            IServerProvider? provider = null;
            foreach (Component comp in Components)
                if (comp is IServerProvider)
                {
                    provider = (IServerProvider)comp;
                    Logger.Trace("Using server provider: " + comp.GetType().FullName);
                    break;
                }
            if (provider == null)
            {
                Logger.Fatal("Cannot start the server! Missing a server implementation component!");
                throw new ArgumentException("No server provider component");
            }
            try
            {
                Logger.Trace("Creating server connection...");
                Server = provider.ProvideServer();
            }
            catch (Exception e)
            {
                Logger.Fatal("Failed to create the server connection! Please check the configuration for typing mistakes.", e);
                throw new ArgumentException("Server creation error");
            }

            // Handshake
            Logger.Trace("Attaching events...");
            GameServer srv = this;
            Server.Connected += (client, args) => {
                Logger.Debug("Late handshake connection: " + client);
                ClientConnectedEvent ev = new ClientConnectedEvent(this, client, args);
                EventBus.Dispatch(ev);
                if (!ev.ShouldKeepConnectionOpen && client.IsConnected())
                    client.Close("disconnect.nofurtherhandlers");
            };
            Server.ConnectionSuccess += (client) =>
            {
                Logger.Debug("Connection success: " + client);
                EventBus.Dispatch(new ClientConnectSuccessEvent(this, client));
            };
            Server.Disconnected += (client, reason, args) => {
                Logger.Debug("Client disconnect: " + client + ": " + reason);
                EventBus.Dispatch(new ClientDisconnectedEvent(this, client, new ClientDisconnectedEventArgs(reason, args)));
            };
            Server.CustomHandshakes += (srvConn, args) => {
                if (args.HasFailed())
                    return;


                //
                // Perform Phoenix handshake
                //

                // Send game ID
                Logger.Trace("Performing Phoenix Game Handshake on connection: " + srvConn);
                Logger.Trace("Sending game ID: " + _gameID + ", protocol version " + _protocol + " to " + srvConn);
                args.ClientOutput.WriteString(_gameID);

                // Send protocol
                args.ClientOutput.WriteInt(_protocol);

                // Read ID and protocol
                string cGID = args.ClientInput.ReadString();
                int cProtocol = args.ClientInput.ReadInt();
                Logger.Trace("Received game ID: " + cGID + ", protocol version " + cProtocol);
                Logger.Trace("Verifying handshake...");
                if (_gameID != cGID)
                {
                    // Fail
                    Logger.Trace("Handshake failure! Game ID mismatch!");
                    args.FailHandshake();
                    return;
                }
                else if (_protocol != cProtocol)
                {
                    // Fail
                    Logger.Trace("Handshake failure! Protocol version mismatch!");
                    args.FailHandshake();
                    return;
                }

                // Read connection IP and port
                string ip = args.ClientInput.ReadString();
                int port = args.ClientInput.ReadInt();
                srvConn.AddObject(new ConnectionInfo(ip, port));
                Logger.Trace("Received connection details: [" + ip + "]:" + port);

                // Assign object
                srvConn.AddObject(srv);
                Logger.Trace("Handshake success!");
            };

            // Initialize components
            Logger.Info("Initializing server components...");
            Phoenix.Common.Logging.Logger.GlobalMessagePrefix += "  ";
            foreach (Component comp in Components)
            {
                Logger.Info("Initializing " + comp.ID);
                Phoenix.Common.Logging.Logger.GlobalMessagePrefix += "  ";
                comp.Init();
                Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);
            }
            Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);

            // Start server
            EventBus.Dispatch(new ServerStartupEvent(this));
            try
            {
                Logger.Trace("Attempting to start server listener...");
                provider.StartGameServer();
                if (!ServerConnection.IsConnected())
                {
                    Logger.Trace("Server listener did not open!");
                    throw new IOException("Server not started");
                }
            }
            catch (Exception e)
            {
                Logger.Fatal("Failed to start the server! Please check the configuration for typing mistakes.", e);
                throw;
            }

            // Post-initialize components
            Running = true;
            Logger.Info("Post-initializing components...");
            Phoenix.Common.Logging.Logger.GlobalMessagePrefix += "  ";
            foreach (Component comp in Components)
            {
                Logger.Info("Post-initializing " + comp.ID);
                Phoenix.Common.Logging.Logger.GlobalMessagePrefix += "  ";
                comp.StartServer();
                Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);
            }
            Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);
            Logger.Info("Server started successfully!");
            EventBus.Dispatch(new ServerStartupCompletedEvent(this));
        }

        private int ticksRun = 0;
        private int tps;
        private bool ticking = false;

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
        /// Checks if the server is initialized
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                return RegistryLocked;
            }
        }

        /// <summary>
        /// Runs the server tick loop
        /// </summary>
        public void ServerLoop()
        {
            if (!Running)
                throw new InvalidOperationException("Server is not running");
            if (ticking)
                throw new InvalidOperationException("Server ticker is already running!");
            ticking = true;

            // Start TPS counter
            Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() => { 
                while (ticking)
                {
                    tps = ticksRun;
                    ticksRun = 0;
                    Thread.Sleep(1000);
                }
            });

            // Start ticker
            try
            {
                long skips = 0;
                while (IsRunning())
                {
                    try
                    {
                        // Tick the world
                        long lastTickStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        // Tick components
                        foreach (Component comp in Components)
                            comp.Tick();
                        OnTick?.Invoke();
                        long tickTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastTickStart;
                        if (!IsRunning())
                            return;
                        if (skips == 0)
                            ticksRun++;
                        if (tickTime > 500)
                        {
                            // This took too long, throw a warning
                            skips += (tickTime / 500) * 10;
                            Logger.Warn("Server tick took too long to complete! Tick took " + tickTime + "ms, fast-forwarding " + skips + " ticks to catch up!");
                        }

                        // Run post-tick
                        OnPostTick?.Invoke();

                        // Sleep
                        if (skips <= 0)
                        {
                            skips = 0;
                            Thread.Sleep(10);
                        }
                        else
                            skips--;
                    }
                    catch (ThreadInterruptedException)
                    {
                        ticking = false;
                        return;
                    }
                }
                ticking = false;
            }
            finally
            {
                ticking = false;
            }
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public void StopServer()
        {
            if (!Running)
                throw new InvalidOperationException("Server is not running");
            Logger.Info("Stopping server...");

            // Stop components
            Logger.Info("Stopping components...");
            Phoenix.Common.Logging.Logger.GlobalMessagePrefix += "  ";
            foreach (Component comp in Components)
            {
                Logger.Info("Stopping " + comp.ID);
                Phoenix.Common.Logging.Logger.GlobalMessagePrefix += "  ";
                comp.StopServer();
                Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);
            }
            Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);

            // Stop server
            Logger.Info("Stopping server...");
            EventBus.Dispatch(new ServerStopEvent(this));
            IServerProvider? provider = null;
            foreach (Component comp in Components)
                if (comp is IServerProvider)
                {
                    provider = (IServerProvider)comp;
                    break;
                }
            if (provider == null)
                throw new ArgumentException("No server provider component");
            provider.StopGameServer();
            Running = false;
            Logger.Info("Server stopped successfully!");
            EventBus.Dispatch(new ServerStoppedEvent(this));
        }

        /// <summary>
        /// Checks if the server is running
        /// </summary>
        public bool IsRunning()
        {
            return Running;
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
                    Logger.Fatal("  Conflict! Component " + component.ID + " conflicts with " + rule.Target + "! Unable to continue!");
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
                    Logger.Fatal("  Missing dependencies!\nComponent " + component.ID + " requires component " + rule.Target + " to be registered.");
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
            Phoenix.Common.Logging.Logger.GlobalMessagePrefix += "  ";
            Logger.Info("Pre-initializing component: " + component.ID);
            component.PreInit();
            EventBus.AttachAll(component);
            Phoenix.Common.Logging.Logger.GlobalMessagePrefix = Phoenix.Common.Logging.Logger.GlobalMessagePrefix.Substring(2);
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

    }
}