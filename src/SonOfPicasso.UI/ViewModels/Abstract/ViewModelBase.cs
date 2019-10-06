using System;
using ReactiveUI;

namespace SonOfPicasso.UI.ViewModels.Abstract
{
    public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel
    {
        protected ViewModelBase(ViewModelActivator activator)
        {
            Activator = activator;
        }

        public ViewModelActivator Activator { get; }
    }
}