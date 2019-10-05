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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Activator?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}