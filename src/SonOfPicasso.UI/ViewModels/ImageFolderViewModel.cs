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
        public ImageFolder ImageFolder { get; private set; }

        public void Initialize(ImageFolder imageFolder)
        {
            ImageFolder = imageFolder;
        }
    }
}