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
        private readonly IBlobCache _blobCache;

        static SharedCache()
        {
            BlobCache.ApplicationName = "SonOfPicasso";
        }

        public SharedCache(ILogger<SharedCache> logger) : this(logger, null)
        {
        }

        protected SharedCache(ILogger<SharedCache> logger,
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
            _blobCache = blobCache ?? GetBlobCacheOrFallback(() => BlobCache.UserAccount, "UserAccount");
        }

        public IObservable<Unit> Clear()
        {
            return _blobCache.InvalidateAll();
        }

        public IObservable<UserSettings> GetUserSettings()
        {
            _logger.LogDebug("GetUserSettings");
            return _blobCache.GetOrCreateObject(UserSettingsKey, () => new UserSettings());
        }

        public IObservable<Unit> SetUserSettings(UserSettings userSettings)
        {
            _logger.LogDebug("SetUserSettings");
            return _blobCache.InsertObject(UserSettingsKey, userSettings);
        }

        public IObservable<ImageFolderDictionary> GetImageFolders()
        {
            _logger.LogDebug("GetImageFolders");
            return _blobCache.GetOrCreateObject(ImageFoldersKey, () => new ImageFolderDictionary());
        }

        public IObservable<Unit> SetImageFolders(ImageFolderDictionary imageFolders)
        {
            _logger.LogDebug("SetImageFolders");
            return _blobCache.InsertObject(ImageFoldersKey, imageFolders);
        }
    }
}