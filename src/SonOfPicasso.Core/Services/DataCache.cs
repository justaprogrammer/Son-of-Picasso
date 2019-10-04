using System;
using System.Reactive;
using Akavache;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;

namespace SonOfPicasso.Core.Services
{
    public class DataCache : IDataCache
    {
        private static UserSettings CreateUserSettings()
        {
            return new UserSettings();
        }

        private const string UserSettingsKey = "UserSettings";

        private readonly ILogger _logger;
        protected readonly IBlobCache BlobCache;

        public DataCache(ILogger logger) : this(logger, null)
        {
        }

        public DataCache(ILogger logger,
            IBlobCache blobCache)
        {
            _logger = logger;
            if (blobCache == null)
            {
                try
                {
                    blobCache = Akavache.BlobCache.UserAccount;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to get the UserAccount cache");
                    throw;
                }
            }

            BlobCache = blobCache;
        }

        public IObservable<Unit> Clear()
        {
            return BlobCache.InvalidateAll();
        }

        public IObservable<UserSettings> GetUserSettings()
        {
            _logger.Debug("GetUserSettings");
            return BlobCache.GetOrCreateObject(UserSettingsKey, CreateUserSettings);
        }

        public IObservable<Unit> SetUserSettings(UserSettings userSettings)
        {
            _logger.Debug("SetUserSettings");
            return BlobCache.InsertObject(UserSettingsKey, userSettings);
        }
    }
}