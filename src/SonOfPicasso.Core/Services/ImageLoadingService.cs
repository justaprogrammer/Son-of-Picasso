﻿using System;
using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using Akavache;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SonOfPicasso.Core.Extensions;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Core.Services
{
    public class ImageLoadingService : IImageLoadingService
    {
        private readonly IBlobCacheProvider _blobCacheProvider;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly string _cacheFolderOverride;
        private IBlobCache _userAccount;

        public ImageLoadingService(IFileSystem fileSystem,
            ILogger logger,
            ISchedulerProvider schedulerProvider,
            IBlobCacheProvider blobCacheProvider) 
            : this(fileSystem, logger, schedulerProvider, blobCacheProvider, null)
        {
        }

        public ImageLoadingService(IFileSystem fileSystem,
            ILogger logger,
            ISchedulerProvider schedulerProvider,
            IBlobCacheProvider blobCacheProvider,
            string cacheFolderOverride)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schedulerProvider = schedulerProvider ?? throw new ArgumentNullException(nameof(schedulerProvider));
            _blobCacheProvider = blobCacheProvider;
            _cacheFolderOverride = cacheFolderOverride;
        }

        public IBlobCache UserAccount => _userAccount ??= _blobCacheProvider.UserAccount;

        public IObservable<BitmapSource> LoadThumbnailFromPath(string path)
        {
            return LoadThumbnailFromPathInternal(path)
                .Select(image => image.CreateBitmapSource());
        }

        public IObservable<Unit> CreateThumbnailFromPath(string path)
        {
            return Observable.Create<Unit>(observer =>
            {
                return LoadThumbnailFromPathInternal(path, true)
                    .Subscribe(image => { },
                        observer.OnError,
                        () =>
                        {
                            observer.OnNext(Unit.Default);
                            observer.OnCompleted();
                        });
            });
        }

        internal IObservable<Image> LoadThumbnailFromPathInternal(string path, bool skipCache = false,
            bool observeOnlyThumbnail = true)
        {
            return Observable.Create<Image>(async (observer, token) =>
            {
                _logger.Verbose("LoadThumbnailFromPathInternal {Path}", path);

                var cacheKey = GetThumbnailKey(path);
                if (!skipCache)
                {
                    var (isCached, cachePath) = await UserAccount.TryGetObject<string>(cacheKey);

                    if (isCached)
                    {
                        if (_fileSystem.File.Exists(cachePath))
                        {
                            var cachedimage = await LoadImageFromPath(cachePath);
                            observer.OnNext(cachedimage);
                            observer.OnCompleted();
                            return Disposable.Empty;
                        }

                        UserAccount.Invalidate(cacheKey).Subscribe();
                    }
                }

                var image = await LoadImageFromPath(path);
                if (!observeOnlyThumbnail) observer.OnNext(image);

                var size = image.Size();
                var (width, height) = size.ResizeKeepAspect(250, 250);

                image.Mutate(context => context.Resize(width, height));
                observer.OnNext(image);

                var cacheFolder = _cacheFolderOverride ??
                                _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "SonOfPicasso", "ImageCache");
                _fileSystem.Directory.CreateDirectory(cacheFolder);

                var thumbnailPath = _fileSystem.Path.Combine(cacheFolder, $"thumbnail_{Guid.NewGuid()}.jpg");

                _logger.Verbose("Caching {Path} to path {Thumbnail}", path, thumbnailPath);

                var directoryName = _fileSystem.Path.GetDirectoryName(thumbnailPath);
                _fileSystem.Directory.CreateDirectory(directoryName);

                image.Save(thumbnailPath);
                await UserAccount.InsertObject(cacheKey, thumbnailPath);
                observer.OnCompleted();

                return Disposable.Empty;
            });
        }

        internal IObservable<Image> LoadImageFromPath(string path)
        {
            return Observable.Defer(() => Observable.Return(Image.Load(path)));
        }

        public static string GetThumbnailKey(string path)
        {
            return $"Thumbnail:{path.ToLowerInvariant()}";
        }
    }
}