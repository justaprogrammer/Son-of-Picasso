using System.IO.Abstractions;

namespace SonOfPicasso.UI.ViewModels
{

    public interface IDirectoryInfoPermissionsService
    {
        bool IsReadable(IDirectoryInfo di);
        bool IsWriteable(IDirectoryInfo me);
    }
}