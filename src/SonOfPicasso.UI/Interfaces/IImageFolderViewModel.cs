using SonOfPicasso.Core.Models;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageFolderViewModel
    {
        void Initialize(ImageFolderModel imageFolderModel);
        string Path { get; }
    }
}