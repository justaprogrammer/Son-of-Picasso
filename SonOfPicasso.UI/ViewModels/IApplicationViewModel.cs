using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.UI.ViewModels
{
    public interface IApplicationViewModel
    {
        void Initialize();
        ReactiveCommand<string, Unit> AddFolder { get; }
        ObservableCollection<ImageFolder> ImageFolders { get; }
    }
}