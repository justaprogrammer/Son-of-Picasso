using System.Reactive.Concurrency;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace SonOfPicasso.UI.WPF.ViewModels.Abstract
{
    public abstract class ValidatedViewModelBase<T> : ReactiveValidationObject<T>, IActivatableViewModel
    {
        protected ValidatedViewModelBase(ViewModelActivator activator, IScheduler scheduler) : base(scheduler)
        {
            Activator = activator;
        }

        public ViewModelActivator Activator { get; }
    }
}