using System;

namespace SonOfPicasso.Core.Services
{
    public static class AspectRatioFactory
    {
        public static (int width, int height) Calculate(int currentWidth, int currentHeight, int maxWidth, int maxHeight,
            bool enlarge)
        {
            // https://stackoverflow.com/a/24491026/104877
       
            maxWidth = enlarge ? maxWidth : Math.Min(maxWidth, currentWidth);
            maxHeight = enlarge ? maxHeight : Math.Min(maxHeight, currentHeight);

            var rnd = Math.Min(maxWidth / (decimal) currentWidth, maxHeight / (decimal) currentHeight);
            var width = (int) Math.Round(currentWidth * rnd);
            var height = (int) Math.Round(currentHeight * rnd);
            var tuple = (width, height);
            return tuple;
        }
    }
}