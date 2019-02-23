using System;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IApplicationViewModel
    {
        IObservable<Unit> Initialize();
        ReactiveCommand<string, Unit> AddFolder { get; }
        ObservableCollection<IImageFolderViewModel> ImageFolders { get; }
    }
}