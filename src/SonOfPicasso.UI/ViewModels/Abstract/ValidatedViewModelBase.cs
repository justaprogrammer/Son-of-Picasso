using System;
using System.Reactive.Concurrency;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace SonOfPicasso.UI.ViewModels.Abstract
{
    public abstract class ValidatedViewModelBase<T> : ReactiveValidationObject<T>, IActivatableViewModel, IDisposable
    {
        protected ValidatedViewModelBase(ViewModelActivator activator, IScheduler scheduler):base(scheduler)
        {
            Activator = activator;
        }

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

        public ViewModelActivator Activator { get; }
    }
}