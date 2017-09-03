using System.Drawing;
using System.Windows.Media.Imaging;
using PicasaReboot.Core.Helpers;

namespace PicasaReboot.Core.Extensions
{
    public static class ImageExtensions
    {
        public static byte[] GetBytes(this Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        public static BitmapImage GetBitmapImage(this Image img)
        {
            var bytes = img.GetBytes();
            return ImageHelpers.LoadImage(bytes);
        }
    }
}