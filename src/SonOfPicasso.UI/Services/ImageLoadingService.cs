using System;
using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using Splat;
using ILogger = Serilog.ILogger;

namespace SonOfPicasso.UI.Services
{
    public class ImageLoadingService : IImageLoadingService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageLoadingService(IFileSystem fileSystem,
            ILogger logger,
            ISchedulerProvider schedulerProvider)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schedulerProvider = schedulerProvider ?? throw new ArgumentNullException(nameof(schedulerProvider));
        }

        public IObservable<BitmapSource> LoadImageFromPath(string path)
        {
            return Observable.DeferAsync(async (token) =>
            {
                _logger.Verbose("Loading image {Path}", path);

                await using var stream = _fileSystem.File.OpenRead(path);
                var load = await BitmapLoader.Current.Load(stream, null, null);
                var result = load.ToNative();
                result.Freeze();

                return Observable.Return(result);
            })
            .SubscribeOn(_schedulerProvider.TaskPool);
        }
    }
}