﻿using System;
using System.IO.Abstractions;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using Splat;

namespace SonOfPicasso.UI.Services
{
    public class ImageLoadingService : IImageLoadingService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<ImageLoadingService> _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageLoadingService(IFileSystem fileSystem,
            ILogger<ImageLoadingService> logger,
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
                        _logger.LogDebug("LoadImageFromPath {Path}", path);
                        return BitmapLoader.Current.Load(stream, null, null);
                    }))
                .SubscribeOn(_schedulerProvider.TaskPool);
        }
    }
}