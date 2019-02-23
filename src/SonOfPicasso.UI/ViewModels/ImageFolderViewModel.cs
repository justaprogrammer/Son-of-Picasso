using ReactiveUI;
using SonOfPicasso.Core.Models;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Views;

namespace SonOfPicasso.UI.ViewModels
{
    [ViewModelView(typeof(ImageFolderViewControl))]
    public class ImageFolderViewModel : ReactiveObject, IImageFolderViewModel
    {
        public string Path { get; private set; }

        public void Initialize(string path)
        {
            Path = path;
        }
    }
}