using System;
using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData;
using ReactiveUI;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IApplicationViewModel: IActivatableViewModel
    {
        ReactiveCommand<string, Unit> AddFolder { get; }
        ObservableCollection<IImageFolderViewModel> ImageFolders { get; }
        ObservableCollection<IImageViewModel> Images { get; }
    }
}