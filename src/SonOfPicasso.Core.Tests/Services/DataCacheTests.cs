using System;
using System.Linq;
using System.Threading;
using Akavache;
using Autofac;
using Autofac.Extras.NSubstitute;
using FluentAssertions;
using NSubstitute;
using Serilog;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class DataCacheTests : TestsBase, IDisposable
    {
        private readonly AutoSubstitute _autoSub;
        private readonly AutoResetEvent _autoResetEvent;

        public DataCacheTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _autoSub = new AutoSubstitute();
            _autoResetEvent = new AutoResetEvent(false);
        }

        public override void Dispose()
        {
            base.Dispose();
            _autoSub.Dispose();
        }

        [Fact]
        public void CanSetUserSettings()
        {
            Logger.Debug("CanSetUserSettings");

            var inMemoryBlobCache = new InMemoryBlobCache();
            _autoSub.Provide<IBlobCache>(inMemoryBlobCache);

            var dataCache = _autoSub.Resolve<DataCache>();

            dataCache.SetUserSettings(new UserSettings())
                .Subscribe(_ => _autoResetEvent.Set());

            _autoResetEvent.WaitOne();

            string[] keys = null;

            inMemoryBlobCache.GetAllKeys()
                .Subscribe(enumerable =>
                {
                    keys = enumerable.ToArray();
                    _autoResetEvent.Set();
                });

            _autoResetEvent.WaitOne();

            keys.Should().NotBeNull();
            keys.Should().Contain("UserSettings");
        }

        [Fact]
        public void CanRetrieveUserSettings()
        {
            Logger.Debug("CanRetrieveUserSettings");

            var inMemoryBlobCache = new InMemoryBlobCache();
            _autoSub.Provide<IBlobCache>(inMemoryBlobCache);

            var dataCache = _autoSub.Resolve<DataCache>();

            var input = new UserSettings();
            dataCache.SetUserSettings(input)
                .Subscribe(_ => _autoResetEvent.Set());

            _autoResetEvent.WaitOne();

            UserSettings output = null;

            dataCache.GetUserSettings()
                .Subscribe(settings =>
                {
                    output = settings;
                    _autoResetEvent.Set();
                });

            _autoResetEvent.WaitOne();

            output.Should().NotBeNull();
            
            // TODO: Remove comment after there are fields in UserSettings
            // output.Should().BeEquivalentTo(input);
        }

        [Fact]
        public void CanCreateUserSettings()
        {
            Logger.Debug("CanCreateUserSettings");

            var inMemoryBlobCache = new InMemoryBlobCache();
            _autoSub.Provide<IBlobCache>(inMemoryBlobCache);

            var dataCache = _autoSub.Resolve<DataCache>();

            UserSettings userSettings = null;

            dataCache.GetUserSettings()
                .Subscribe(settings =>
                {
                    userSettings = settings;
                    _autoResetEvent.Set();
                });

            _autoResetEvent.WaitOne();

            userSettings.Should().NotBeNull();
        }
    }
}