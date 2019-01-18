using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Splat;

namespace SonOfPicasso.UI
{
    public class SplatDependencyResolver : IMutableDependencyResolver
    {
        protected internal const string ContractsAreNotSupported = "Contracts are not supported";
        protected internal const string ServiceProviderNotSet = "ServiceProvider not set";

        private readonly IServiceCollection _serviceCollection;
        private ServiceProvider _serviceProvider;

        public SplatDependencyResolver(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        void IMutableDependencyResolver.Register(Func<object> factory, Type serviceType, string contract)
        {
            _serviceCollection.AddTransient(serviceType, provider => factory());
        }

        void IMutableDependencyResolver.UnregisterCurrent(Type serviceType, string contract)
        {
            throw new NotSupportedException();
        }

        void IMutableDependencyResolver.UnregisterAll(Type serviceType, string contract)
        {
            throw new NotSupportedException();
        }

        IDisposable IMutableDependencyResolver.ServiceRegistrationCallback(Type serviceType, string contract, Action<IDisposable> callback)
        {
            throw new NotSupportedException();
        }

        object IDependencyResolver.GetService(Type serviceType, string contract)
        {
            GuardContractUsage(contract);
            
            return ServiceProvider.GetService(serviceType);
        }

        IEnumerable<object> IDependencyResolver.GetServices(Type serviceType, string contract)
        {
            GuardContractUsage(contract);

            return ServiceProvider.GetServices(serviceType);
        }

        private static void GuardContractUsage(string contract)
        {
            if (contract != null)
            {
                throw new NotSupportedException(ContractsAreNotSupported);
            }
        }

        public ServiceProvider ServiceProvider
        {
            get
            {
                if (this._serviceProvider == null)
                {
                    throw new InvalidOperationException(ServiceProviderNotSet);
                }

                return _serviceProvider;
            }
            set { this._serviceProvider = value; }
        }

        void IDisposable.Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}