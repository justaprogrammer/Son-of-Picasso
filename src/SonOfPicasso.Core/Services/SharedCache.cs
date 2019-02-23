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

        public IObservable<string[]> GetFolderList()
        {
            _logger.LogDebug("GetFolderList");
            return BlobCache.GetOrCreateObject(ImageFoldersKey, Array.Empty<string>);
        }

        public IObservable<Unit> SetFolderList(string[] paths)
        {
            _logger.LogDebug("SetFolderList");
            return BlobCache.InsertObject(ImageFoldersKey, paths);
        }

        public IObservable<ImageFolder> GetFolder(string path)
        {
            _logger.LogDebug("GetFolder");
            return BlobCache.GetOrCreateObject(GetImageFolderDetailKey(path), () => new ImageFolder());
        }

        public IObservable<Unit> SetFolder(ImageFolder imageFolder)
        {
            _logger.LogDebug("SetFolder");
            return BlobCache.InsertObject(GetImageFolderDetailKey(imageFolder.Path), imageFolder);
        }

        private static string GetImageFolderDetailKey(string path) => $"ImageFolder {path}";
    }
}