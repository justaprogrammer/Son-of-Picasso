using SonOfPicasso.Core.Models;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageFolderViewModel
    {
        void Initialize(Directory directory);
        string Path { get; }
    }
}