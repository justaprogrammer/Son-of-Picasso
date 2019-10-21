using System.Windows.Media.Imaging;

namespace SonOfPicasso.UI.Interfaces
{
    public interface ISvgImageProvider
    {
        BitmapImage Folder { get; }
        BitmapImage OpenedFolder { get; }
        BitmapImage Cancel { get; }
        BitmapImage Checkmark { get; }
        BitmapImage Synchronize { get; }
    }
}