using System;
using System.Reactive;
using Akavache;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Services
{
    public class SharedCache : ISharedCache
    {
        private const string UserSettingsKey = "UserSettings";
        private const string ImageFoldersKey = "ImageFolders";

        private readonly ILogger<SharedCache> _logger;
        protected readonly IBlobCache BlobCache;

        static SharedCache()
        {
            Akavache.BlobCache.ApplicationName = "SonOfPicasso";
        }

        public SharedCache(ILogger<SharedCache> logger) : this(logger, null)
        {
        }

        internal SharedCache(ILogger<SharedCache> logger,
            IBlobCache blobCache)
        {
            IBlobCache GetBlobCacheOrFallback(Func<IBlobCache> blobCacheFunc, string cacheName)
            {
                try
                {
                    return blobCacheFunc();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to set the {CacheName} cache", cacheName);
                    return new InMemoryBlobCache();
                }
            }

            _logger = logger;
            BlobCache = blobCache ?? GetBlobCacheOrFallback(() => Akavache.BlobCache.UserAccount, "UserAccount");
        }

        public IObservable<Unit> Clear()
        {
            return BlobCache.InvalidateAll();
        }

        public IObservable<UserSettings> GetUserSettings()
        {
            _logger.LogDebug("GetUserSettings");
            return BlobCache.GetOrCreateObject(UserSettingsKey, () => new UserSettings());
        }

        public IObservable<Unit> SetUserSettings(UserSettings userSettings)
        {
            _logger.LogDebug("SetUserSettings");
            return BlobCache.InsertObject(UserSettingsKey, userSettings);
        }

        public IObservable<ImageFolderDictionary> GetImageFolders()
        {
            _logger.LogDebug("GetImageFolders");
            return BlobCache.GetOrCreateObject(ImageFoldersKey, () => new ImageFolderDictionary());
        }

        public IObservable<Unit> SetImageFolders(ImageFolderDictionary imageFolders)
        {
            _logger.LogDebug("SetImageFolders");
            return BlobCache.InsertObject(ImageFoldersKey, imageFolders);
        }
    }
}