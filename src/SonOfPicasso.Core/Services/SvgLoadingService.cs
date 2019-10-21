using System;
using System.Drawing;
using System.Reflection;
using SonOfPicasso.Core.Interfaces;
using Svg;

namespace SonOfPicasso.Core.Services
{
    public class SvgLoadingService : ISvgLoadingService
    {
        public Bitmap Load(string name, Type resourceAssemblyType)
        {
            var resourceStream = Assembly
                .GetAssembly(resourceAssemblyType)
                .GetManifestResourceStream(name);

            if (resourceStream == null) throw new InvalidOperationException($"Resource '{name}' is not found.");

            SvgDocument svgDocument;
            using (resourceStream)
            {
                svgDocument = SvgDocument.Open<SvgDocument>(resourceStream);
            }

            return svgDocument.Draw();
        }
    }
}