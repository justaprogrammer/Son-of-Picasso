using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Splat;

namespace SonOfPicasso.UI.Injection
{
    [ExcludeFromCodeCoverage]
    public class SplatDependencyResolver : IMutableDependencyResolver
    {
        private readonly HashSet<Type> _serviceProviderLookup;
        private readonly IServiceProvider _serviceProvider;
        private readonly ModernDependencyResolver _modernDependencyResolver = new ModernDependencyResolver();

        public SplatDependencyResolver(IServiceCollection serviceCollection, IServiceProvider serviceProvider)
        {
            var collection = serviceCollection
                    .Select(descriptor => descriptor.ServiceType)
                    .Distinct();

            _serviceProviderLookup = new HashSet<Type>(collection);
            _serviceProvider = serviceProvider;
        }

        public void Dispose()
        {
            _modernDependencyResolver.Dispose();
        }

        public object GetService(Type serviceType, string contract = null)
        {
            if (_serviceProviderLookup.Contains(serviceType))
            {
                return _serviceProvider.GetService(serviceType);
            }
        
            return _modernDependencyResolver.GetService(serviceType, contract);
        }

        public IEnumerable<object> GetServices(Type serviceType, string contract = null)
        {
            if (_serviceProviderLookup.Contains(serviceType))
            {
                return _serviceProvider.GetServices(serviceType);
            }
        
            return _modernDependencyResolver.GetServices(serviceType, contract);
        }

        public void Register(Func<object> factory, Type serviceType, string contract = null)
        {
            _modernDependencyResolver.Register(factory, serviceType, contract);
        }

        public void UnregisterCurrent(Type serviceType, string contract = null)
        {
            _modernDependencyResolver.UnregisterCurrent(serviceType, contract);
        }

        public void UnregisterAll(Type serviceType, string contract = null)
        {
            _modernDependencyResolver.UnregisterAll(serviceType, contract);
        }

        public IDisposable ServiceRegistrationCallback(Type serviceType, string contract, Action<IDisposable> callback)
        {
            return _modernDependencyResolver.ServiceRegistrationCallback(serviceType, contract, callback);
        }
    }
}