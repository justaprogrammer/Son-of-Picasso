using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IApplicationViewModel: IActivatableViewModel
    {
        ReactiveCommand<string, Unit> AddFolder { get; }
        ReactiveCommand<Unit, Unit> NewAlbum { get; }
        ObservableCollection<IImageFolderViewModel> ImageFolders { get; }
        ObservableCollection<IImageViewModel> Images { get; }
    }
}