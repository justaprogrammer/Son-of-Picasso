using System.Windows.Media.Imaging;
using SonOfPicasso.Core.Models;
using Splat;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageViewModel
    {
        void Initialize(Image image);
        Image Image { get; }
        BitmapSource Bitmap { get; }
    }
}