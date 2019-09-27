using System.Reactive;
using ReactiveUI;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IAddAlbumViewModel: IActivatableViewModel
    {
        ReactiveCommand<Unit, Unit> Continue { get; }
        string AlbumName { get; set; }
    }
}