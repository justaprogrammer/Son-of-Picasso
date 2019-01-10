using System;
using Akavache;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.Core.Services
{
    public class SharedCache : ISharedCache
    {
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
            Logger = logger;
            UserAccount = userAccountCache ?? GetBlobCacheWithFallback(() => BlobCache.UserAccount, "UserAccount");
            LocalMachine = localMachineCache ?? GetBlobCacheWithFallback(() => BlobCache.LocalMachine, "LocalMachine");
        }

        public IBlobCache UserAccount { get; }
        public IBlobCache LocalMachine { get; }
        public ILogger<SharedCache> Logger { get; }

        IBlobCache GetBlobCacheWithFallback(Func<IBlobCache> blobCacheFunc, string cacheName)
        {
            try
            {
                return blobCacheFunc();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to set the {CacheName} cache", cacheName);
                return new InMemoryBlobCache();
            }
        }
    }
}