using ReactiveUI;

namespace SonOfPicasso.UI.WPF.ViewModels.Abstract
{
    public abstract class ActivatableViewModelBase : ViewModelBase, IActivatableViewModel
    {
        protected ActivatableViewModelBase(ViewModelActivator activator)
        {
            Activator = activator;
        }

        public ViewModelActivator Activator { get; }
    }
}