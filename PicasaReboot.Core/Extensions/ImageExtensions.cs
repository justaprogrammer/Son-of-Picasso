using System.Drawing;

namespace PicasaReboot.Core.Extensions
{
    public static class ImageExtensions
    {
        public static byte[] ImageToBytes(this Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
    }
}