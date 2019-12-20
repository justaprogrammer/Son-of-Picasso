using System.Drawing;
using SonOfPicasso.Core.Services;

namespace SonOfPicasso.Core.Extensions
{
    public static class SizeExtensions
    {
        public static Size ResizeKeepAspect(this Size src, int maxWidth, int maxHeight, bool enlarge = false)
        {
            var srcWidth = src.Width;
            var srcHeight = src.Height;

            var (width, height) = AspectRatioFactory.Calculate(srcWidth, srcHeight, maxWidth, maxHeight, enlarge);

            return new Size(width, height);
        }

        public static (int width, int height) ResizeKeepAspect(this SixLabors.Primitives.Size size, int maxWidth, int maxHeight)
        {
            return AspectRatioFactory.Calculate(size.Width, size.Height, maxWidth, maxHeight, false);
        }
    }
}