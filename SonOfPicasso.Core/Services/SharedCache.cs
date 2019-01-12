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
        private readonly IBlobCache _userAccount;
        private readonly IBlobCache _localMachine;

        static SharedCache()
        {
            BlobCache.ApplicationName = "SonOfPicasso";
        }

        public SharedCache(ILogger<SharedCache> logger) : this(logger, null, null)
        {
        }

        protected SharedCache(ILogger<SharedCache> logger,
            IBlobCache userAccountCache,
            IBlobCache localMachineCache)
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
            _userAccount = userAccountCache ?? GetBlobCacheOrFallback(() => BlobCache.UserAccount, "UserAccount");
            _localMachine = localMachineCache ?? GetBlobCacheOrFallback(() => BlobCache.LocalMachine, "LocalMachine");
        }

        public IObservable<UserSettings> GetUserSettings()
        {
            _logger.LogDebug("GetUserSettings");
            return _userAccount.GetOrCreateObject(UserSettingsKey, () => new UserSettings());
        }

        public IObservable<Unit> SetUserSettings(UserSettings userSettings)
        {
            _logger.LogDebug("SetUserSettings");
            return _userAccount.InsertObject(UserSettingsKey, userSettings);
        }

        public IObservable<ImageFolderDictionary> GetImageFolders()
        {
            _logger.LogDebug("GetImageFolders");
            return _userAccount.GetOrCreateObject(ImageFoldersKey, () => new ImageFolderDictionary());
        }

        public IObservable<Unit> SetImageFolders(ImageFolderDictionary imageFolders)
        {
            _logger.LogDebug("SetImageFolders");
            return _userAccount.InsertObject(ImageFoldersKey, imageFolders);
        }
    }
}