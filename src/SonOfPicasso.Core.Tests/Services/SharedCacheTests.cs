using System;
using System.Linq;
using System.Threading;
using Akavache;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
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

            var input = new UserSettings();
            sharedCache.SetUserSettings(input)
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            UserSettings output = null;

            sharedCache.GetUserSettings()
                .Subscribe(settings =>
                {
                    output = settings;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            output.Should().NotBeNull();
            // output.Should().BeEquivalentTo(input);
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

        [Fact]
        public void CanSetFolderList()
        {
            Logger.LogDebug("CanSetUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            sharedCache.SetFolderList(new string[0])
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
            keys.Should().Contain("ImageFolders");
        }

        [Fact]
        public void CanRetrieveFolderList()
        {
            Logger.LogDebug("CanRetrieveUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            var input = Faker.Lorem.Words();

            sharedCache.SetFolderList(input)
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            string[] output = null;

            sharedCache.GetFolderList()
                .Subscribe(folders =>
                {
                    output = folders;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            output.Should().NotBeNull();
            output.Should().BeEquivalentTo(input);
        }

        [Fact]
        public void CanCreateFolderList()
        {
            Logger.LogDebug("CanCreateUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            string[] output = null;

            sharedCache.GetFolderList()
                .Subscribe(folders =>
                {
                    output = folders;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            output.Should().NotBeNull();
            output.Should().BeEmpty();
        }

        [Fact]
        public void CanSetImageFolder()
        {
            Logger.LogDebug("CanSetUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            var imageFolder = DataGenerator.ImageFolderFaker.Generate();

            sharedCache.SetFolder(imageFolder)
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
            keys.Should().Contain($"ImageFolder {imageFolder.Path}");
        }

        [Fact]
        public void CanRetrieveImageFolder()
        {
            Logger.LogDebug("CanRetrieveUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            var input = DataGenerator.ImageFolderFaker.Generate();

            sharedCache.SetFolder(input)
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            ImageFolder output = null;

            sharedCache.GetFolder(input.Path)
                .Subscribe(folder =>
                {
                    output = folder;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            output.Should().NotBeNull();
            output.Should().BeEquivalentTo(input);
        }

        [Fact]
        public void CanCreateImageFolder()
        {
            Logger.LogDebug("CanCreateUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);

            ImageFolder output = null;

            var path = Faker.System.DirectoryPathWindows();

            sharedCache.GetFolder(path)
                .Subscribe(folder =>
                {
                    output = folder;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne();

            output.Should().NotBeNull();
            output.Path.Should().Be(path);
            output.Images.Should().BeEmpty();
        }
    }
}
