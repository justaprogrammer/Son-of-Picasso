using ReactiveUI;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class TrayImageViewModel : ViewModelBase
    {
        public TrayImageViewModel(ViewModelActivator activator) : base(activator)
        {
        }

        public ImageViewModel Image { get; private set; }

        public void Initialize(ImageViewModel imageViewModel)
        {
            Image = imageViewModel;
        }
    }
}