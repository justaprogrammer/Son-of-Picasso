using System.IO;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;

namespace SonOfPicasso.Core.Services
{
    public static class ImageExtensions
    {
        public static BitmapSource CreateBitmapSource(this Image image)
        {
            var memoryStream = new MemoryStream();
            image.SaveAsPng(memoryStream);
            memoryStream.Position = 0;
            
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }
}