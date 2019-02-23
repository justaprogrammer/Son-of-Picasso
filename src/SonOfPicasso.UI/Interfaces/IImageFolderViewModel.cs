using SonOfPicasso.Core.Models;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageFolderViewModel
    {
        void Initialize(string path);
        string Path { get; }
    }
}