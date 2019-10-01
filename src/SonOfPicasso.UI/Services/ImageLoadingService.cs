﻿using System;
using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Threading.Tasks;
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

        public IObservable<IBitmap> LoadImageFromPath(string path)
        {
            return Observable.Using(
                () => _fileSystem.File.OpenRead(path),
                stream => Observable.FromAsync(
                    () =>
                    {
                        _logger.Debug("LoadImageFromPath {Path}", path);
                        return BitmapLoader.Current.Load(stream, null, null);
                    }))
                .SubscribeOn(_schedulerProvider.TaskPool);
        }
    }
}