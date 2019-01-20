using System;
using System.Threading.Tasks;
using Splat;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageLoadingService
    {
        IObservable<IBitmap> LoadImageFromPath(string path);
    }
}