using System;
using ReactiveUI;

namespace SonOfPicasso.UI.ViewModels.Abstract
{
    public abstract class ViewModelBase : ReactiveObject, IDisposable, IActivatableViewModel
    {
        protected ViewModelBase(ViewModelActivator activator)
        {
            Activator = activator;
        }

        public ViewModelActivator Activator { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) Activator?.Dispose();
        }
    }
}