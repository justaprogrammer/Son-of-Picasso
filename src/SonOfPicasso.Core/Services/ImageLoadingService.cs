using System;
using System.IO;
using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Interfaces;
using Splat;

namespace SonOfPicasso.Core.Services
{
    public class ImageLoadingService : IImageLoadingService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<ImageLoadingService> _logger;

        public ImageLoadingService(IFileSystem fileSystem, ILogger<ImageLoadingService> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public IObservable<IBitmap> LoadImageFromPath(string path)
        {
            _logger.LogDebug("LoadImageFromPath {Path}", path);

            return Observable.Using(
                () => _fileSystem.File.OpenRead(path),
                stream => Observable.FromAsync(() => BitmapLoader.Current.Load(stream, null, null)));
        }
    }
}