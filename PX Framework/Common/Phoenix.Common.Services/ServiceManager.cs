using Phoenix.Common.Logging;

namespace Phoenix.Common.Services {
    /// <summary>
    /// Service Manager
    /// </summary>
    public class ServiceManager {

        internal List<IService> Services = new List<IService>();

        /// <summary>
        /// Retrieves a service by type
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>Service instance</returns>
        public T GetService<T>() where T : IService {
            Type serviceType = typeof(T);

            foreach (IService service in Services) {
                if (serviceType.IsAssignableFrom(service.GetType()))
                    return (T)service;
            }

            throw new ArgumentException("Service not registered");
        }

        /// <summary>
        /// Registers a service
        /// </summary>
        /// <param name="service">Service to register</param>
        public void RegisterService(IService service)
        {
            if (Services.Any(t => t.GetType().IsAssignableFrom(service.GetType())))
                return;
            Logger.GetLogger("service-manager").Trace("Registering service: " + service.GetType().FullName + "...");
            Services.Add(service);
        }

    }

    /// <summary>
    /// Service interface
    /// </summary>
    public interface IService {
    }
}