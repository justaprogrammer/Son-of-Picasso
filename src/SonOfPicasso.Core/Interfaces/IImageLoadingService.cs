using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Splat;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageLoadingService
    {
        IObservable<BitmapSource> LoadImageFromPath(string path);
    }
}