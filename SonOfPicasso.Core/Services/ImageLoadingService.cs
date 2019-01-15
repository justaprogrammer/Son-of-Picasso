using System;
using System.IO;
using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using SonOfPicasso.Core.Interfaces;
using Splat;

namespace SonOfPicasso.Core.Services
{
    public class ImageLoadingService : IImageLoadingService
    {
        private readonly IFileSystem _fileSystem;

        public ImageLoadingService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IObservable<IBitmap> LoadImageFromPath(string path)
        {
            return Observable.Using(
                () => _fileSystem.File.OpenRead(path),
                stream => Observable.FromAsync(() => BitmapLoader.Current.Load(stream, null, null)));
        }
    }
}