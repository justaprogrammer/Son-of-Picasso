using System;
using System.Drawing;

namespace SonOfPicasso.Core.Interfaces
{
    public interface ISvgLoadingService
    {
        Bitmap Load(string name, Type resourceAssemblyType);
    }
}