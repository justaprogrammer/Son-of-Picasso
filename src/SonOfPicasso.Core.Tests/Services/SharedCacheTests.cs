using System;
using System.Linq;
using System.Threading;
using Akavache;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class SharedCacheTests : TestsBase<SharedCacheTests>
    {
        public SharedCacheTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.LogDebug("CanInitialize");

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);
        }

        [Fact]
        public void CanSetUserSettings()
        {
            Logger.LogDebug("CanSetUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            sharedCache.SetUserSettings(new UserSettings())
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            string[] keys = null;

            inMemoryBlobCache.GetAllKeys()
                .Subscribe(enumerable =>
                {
                    keys = enumerable.ToArray();
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            keys.Should().NotBeNull();
            keys.Should().Contain("UserSettings");
        }

        [Fact]
        public void CanRetrieveUserSettings()
        {
            Logger.LogDebug("CanRetrieveUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            sharedCache.SetUserSettings(new UserSettings())
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            UserSettings userSettings = null;

            sharedCache.GetUserSettings()
                .Subscribe(settings =>
                {
                    userSettings = settings;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            userSettings.Should().NotBeNull();
        }

        [Fact]
        public void CanCreateUserSettings()
        {
            Logger.LogDebug("CanCreateUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            UserSettings userSettings = null;

            sharedCache.GetUserSettings()
                .Subscribe(settings =>
                {
                    userSettings = settings;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            userSettings.Should().NotBeNull();
        }
    }
}
