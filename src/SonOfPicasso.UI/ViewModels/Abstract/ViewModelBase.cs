using System;
using System.Collections.Generic;
using ReactiveUI;

namespace SonOfPicasso.UI.ViewModels.Abstract
{
    public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel, IDisposable
    {
        protected ViewModelBase(ViewModelActivator activator)
        {
            Activator = activator;
        }

        public ViewModelActivator Activator { get; }

        public void Dispose()
        {
            Activator?.Dispose();
        }
    }
}