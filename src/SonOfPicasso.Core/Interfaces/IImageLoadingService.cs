using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Splat;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageLoadingService
    {
        IObservable<BitmapSource> LoadThumbnailFromPath(string path);
        IObservable<Unit> CreateThumbnailFromPath(string path);
    }
}