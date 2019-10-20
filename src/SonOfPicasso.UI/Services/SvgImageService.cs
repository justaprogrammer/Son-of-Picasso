using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SonOfPicasso.UI.Interfaces;
using Svg;

namespace SonOfPicasso.UI.Services
{
    public class SvgImageService : ISvgImageService
    {
        public Bitmap LoadBitmap(string name)
        {
            return LoadSvgDocument(name).Draw();
        }

        public BitmapImage LoadBitmapImage(string name)
        {
            var memoryStream = new MemoryStream();
            var svgDocument = LoadBitmap(name);
            svgDocument.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        private static SvgDocument LoadSvgDocument(string name)
        {
            var resourceStream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream($"SonOfPicasso.UI.Resources.{name}.svg");

            if (resourceStream == null) throw new InvalidOperationException($"Resource '{name}' is not found.");

            SvgDocument svgDocument;
            using (resourceStream)
            {
                svgDocument = SvgDocument.Open<SvgDocument>(resourceStream);
            }

            return svgDocument;
        }
    }
}