﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ReactiveUI;

namespace SonOfPicasso.UI.Injection
{
    [ExcludeFromCodeCoverage]
    public class CustomViewLocator : IViewLocator
    {
        public IViewFor ResolveView<T>(T viewModel, string contract = null) where T : class
        {
            var type = viewModel.GetType();
            var viewModelViewAttribute = type.GetCustomAttribute<ViewModelViewAttribute>();
            var viewFor = (IViewFor)ServiceProvider.GetService(viewModelViewAttribute.Type);
            viewFor.ViewModel = viewModel;
            return viewFor;
        }

        public static IServiceProvider ServiceProvider { get; set; }
    }
}