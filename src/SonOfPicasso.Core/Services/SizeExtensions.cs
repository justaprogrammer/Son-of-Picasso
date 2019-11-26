using System;
using System.Drawing;

namespace SonOfPicasso.Core.Services
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
    }
}