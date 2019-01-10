using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;

namespace SonOfPicasso.UI.ViewModels
{
    public interface IApplicationViewModel
    {
        void Initialize();
        ReactiveCommand<string, Unit> AddFolder { get; }
        ObservableCollection<string> Paths { get; set; }
    }
}