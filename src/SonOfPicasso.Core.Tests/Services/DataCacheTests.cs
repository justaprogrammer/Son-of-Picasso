using System;
using System.Linq;
using System.Threading;
using Akavache;
using FluentAssertions;
using NSubstitute;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class DataCacheTests : TestsBase<DataCacheTests>
    {
        public DataCacheTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.Debug("CanInitialize");

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);
        }

        [Fact]
        public void CanSetUserSettings()
        {
            Logger.Debug("CanSetUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            dataCache.SetUserSettings(new UserSettings())
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
            Logger.Debug("CanRetrieveUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            var input = new UserSettings();
            dataCache.SetUserSettings(input)
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            UserSettings output = null;

            dataCache.GetUserSettings()
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
            Logger.Debug("CanCreateUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            UserSettings userSettings = null;

            dataCache.GetUserSettings()
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
            Logger.Debug("CanSetUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            dataCache.SetFolderList(new string[0])
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
            Logger.Debug("CanRetrieveUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            var input = Faker.Lorem.Words();

            dataCache.SetFolderList(input)
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            string[] output = null;

            dataCache.GetFolderList()
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
            Logger.Debug("CanCreateUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            string[] output = null;

            dataCache.GetFolderList()
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
            Logger.Debug("CanSetUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            var imageFolder = DataGenerator.ImageFolderFaker.Generate();

            dataCache.SetFolder(imageFolder)
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
            Logger.Debug("CanRetrieveUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            var input = DataGenerator.ImageFolderFaker.Generate();

            dataCache.SetFolder(input)
                .Subscribe(_ => autoResetEvent.Set());

            autoResetEvent.WaitOne();

            ImageFolderModel output = null;

            dataCache.GetFolder(input.Path)
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
            Logger.Debug("CanCreateUserSettings");

            var autoResetEvent = new AutoResetEvent(false);

            var inMemoryBlobCache = new InMemoryBlobCache();
            var dataCache = this.CreateDataCache(inMemoryBlobCache);

            ImageFolderModel output = null;

            var path = Faker.System.DirectoryPathWindows();

            dataCache.GetFolder(path)
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
