using System.Reactive;
using ReactiveUI;

namespace SonOfPicasso.UI.ViewModels
{
    public interface IApplicationViewModel
    {
        void Initialize();
        string PathToImages { get; set; }
        ReactiveCommand<Unit, Unit> BrowseToDatabase { get; }
    }
}