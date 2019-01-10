﻿using System;
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
        private readonly ILogger<SharedCache> _logger;
        private readonly IBlobCache _userAccount;
        private readonly IBlobCache _localMachine;

        static SharedCache()
        {
            try
            {
                BlobCache.ApplicationName = "SonOfPicasso";
            }
            catch (Exception e)
            {
//                log.Error(e, "Error while running the static inializer for SharedCache");
            }
        }

        public SharedCache(ILogger<SharedCache> logger) : this(logger, null, null)
        {
        }

        protected SharedCache(ILogger<SharedCache> logger,
            IBlobCache userAccountCache,
            IBlobCache localMachineCache)
        {
            _logger = logger;
            _userAccount = userAccountCache ?? GetBlobCacheWithFallback(() => BlobCache.UserAccount, "UserAccount");
            _localMachine = localMachineCache ?? GetBlobCacheWithFallback(() => BlobCache.LocalMachine, "LocalMachine");
        }

        private IBlobCache GetBlobCacheWithFallback(Func<IBlobCache> blobCacheFunc, string cacheName)
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
    }
}