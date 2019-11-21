using System;
using System.IO;
using System.IO.Abstractions;
using System.Reactive.Linq;
using Akavache;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using Splat;
using ILogger = Serilog.ILogger;

namespace SonOfPicasso.UI.Services
{
    public class ImageLoadingService : IImageLoadingService
    {
        private readonly IBlobCacheProvider _blobCacheProvider;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageLoadingService(IFileSystem fileSystem,
            ILogger logger,
            ISchedulerProvider schedulerProvider,
            IBlobCacheProvider blobCacheProvider)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schedulerProvider = schedulerProvider ?? throw new ArgumentNullException(nameof(schedulerProvider));
            _blobCacheProvider = blobCacheProvider ?? throw new ArgumentNullException(nameof(blobCacheProvider));
        }

        public IObservable<IBitmap> LoadImageFromPath(string path)
        {
            return _blobCacheProvider.LocalMachine
                .GetOrCreateObject(GetImageKey(path), () =>
                {
                    _logger.Verbose("Loading Image {Path}", path);
                    return _fileSystem.File.ReadAllBytes(path);
                }, DateTimeOffset.Now.AddDays(7))
                .SelectMany(bytes =>
                {
                    var sourceStream = new MemoryStream(bytes);
                    return BitmapLoader.Current.Load(sourceStream, null, null);
                })
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        private static string GetImageKey(string path)
        {
            return $"LoadImage{path}";
        }
    }
}