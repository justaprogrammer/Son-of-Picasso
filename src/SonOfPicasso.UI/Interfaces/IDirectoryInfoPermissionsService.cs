using System.IO.Abstractions;

namespace SonOfPicasso.UI.WPF.Interfaces
{

    public interface IDirectoryInfoPermissionsService
    {
        bool IsReadable(IDirectoryInfo di);
        bool IsWriteable(IDirectoryInfo me);
    }
}