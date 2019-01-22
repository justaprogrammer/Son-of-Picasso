using System.Drawing;
using Skybrud.Colors;

namespace SonOfPicasso.Tools.Extensions
{
    public static class HslColorExtensions
    {
        public static Color ToColor(this HslColor color0)
        {
            var rgbColor = color0.ToRgb();
            return Color.FromArgb(rgbColor.R, rgbColor.G, rgbColor.B);
        }
    }
}