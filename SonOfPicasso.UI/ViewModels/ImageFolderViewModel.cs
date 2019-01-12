using ReactiveUI;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageFolderViewModel : ReactiveObject, IImageFolderViewModel
    {
        public ImageFolder ImageFolder { get; private set; }

        public void Initialize(ImageFolder imageFolder)
        {
            ImageFolder = imageFolder;
        }
    }
}