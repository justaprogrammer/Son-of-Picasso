using ReactiveUI;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class TrayImageViewModel : ViewModelBase
    {
        private bool _pinned;

        public TrayImageViewModel(ImageViewModel imageViewModel)
        {
            ImageViewModel = imageViewModel;
        }

        public ImageViewModel ImageViewModel { get; }

        public bool Pinned
        {
            get => _pinned;
            set => this.RaiseAndSetIfChanged(ref _pinned, value);
        }
    }
}