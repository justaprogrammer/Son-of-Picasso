using System;
using System.Reflection;
using Autofac;
using ReactiveUI;

namespace SonOfPicasso.UI.Injection
{
    public class CustomViewLocator : IViewLocator
    {
        private readonly ILifetimeScope _lifetimeScope;

        public CustomViewLocator(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public IViewFor ResolveView<T>(T viewModel, string contract = null) where T : class
        {
            var type = viewModel.GetType();
            var viewModelViewAttribute = type.GetCustomAttribute<ViewModelViewAttribute>();
            var genericFactoryType = typeof(Func<>).MakeGenericType(viewModelViewAttribute.Type);
            var resolve = (Func<object>)_lifetimeScope.Resolve(genericFactoryType);
            var viewFor = (IViewFor)resolve.Invoke();
            viewFor.ViewModel = viewModel;
            return viewFor;
        }
    }
}