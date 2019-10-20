using System.Drawing;
using System.Windows.Media.Imaging;

namespace SonOfPicasso.UI.Interfaces
{
    public interface ISvgImageService
    {
        Bitmap LoadBitmap(string name);
        BitmapImage LoadBitmapImage(string name);
    }
}