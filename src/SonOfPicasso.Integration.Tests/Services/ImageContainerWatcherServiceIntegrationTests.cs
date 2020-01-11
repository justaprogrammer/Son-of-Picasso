using Autofac;
using DynamicData;
using FluentAssertions;
using FluentAssertions.Execution;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class ImageContainerWatcherServiceIntegrationTests : IntegrationTestsBase
    {
        public ImageContainerWatcherServiceIntegrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            var containerBuilder = GetContainerBuilder();

            containerBuilder.RegisterType<ImageContainerWatcherService>();
            containerBuilder.RegisterType<ImageLocationService>()
                .As<IImageLocationService>();
            containerBuilder.RegisterType<FolderRulesManagementService>()
                .As<IFolderRulesManagementService>();

            Container = containerBuilder.Build();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Container.Dispose();
            }
        }

        protected override IContainer Container { get; }

        private ImageRef CreateImageRef(string imagePath)
        {
            return new ImageRef(Faker.Random.Int(), imagePath, Faker.Date.Recent(), Faker.Date.Recent(),
                Faker.Date.Recent(), Faker.Random.Int(), Faker.Random.String(),
                Faker.PickRandom<ImageContainerTypeEnum>(), Faker.Date.Recent());
        }

        [Fact]
        public void ShouldStartWithNoRules()
        {
            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            imageContainerWatcherService.Start(imageRefCache, Array.Empty<string>());
        }

        [Fact]
        public async Task ShouldDetectCreatedOrChangedFiles()
        {
            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var set = new HashSet<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(item =>
            {
                set.Add(item);
                Logger.Verbose("File discovered '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[]{ImagesPath});
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            await GenerateImagesAsync(1).ConfigureAwait(false);

            WaitOne(45);

            set.Should().HaveCount(1);

            imageContainerWatcherService.Stop();
        }

        [Fact]
        public async Task ShouldDetectUpdatedFiles()
        {
            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var set = new HashSet<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(item =>
            {
                set.Add(item);
                Logger.Verbose("File discovered '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[]{ImagesPath});
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            var generatedImages = await GenerateImagesAsync(1).ConfigureAwait(false);
            var path = generatedImages.First().Value.First();

            WaitOne(45);

            using (new AssertionScope())
            {
                set.Should().HaveCount(1);
                set.First().Should().Be(path);
            }

            await ImageGenerationService.GenerateImage(path,
                Fakers.ExifDataFaker);

            WaitOne(45);

            using (new AssertionScope())
            {
                set.Should().HaveCount(1);
                set.First().Should().Be(path);
            }

            imageContainerWatcherService.Stop();
        }

        [Fact]
        public async Task ShouldDetectUnknownCreatedOrChangedFiles()
        {
            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);

            var generatedImages = await GenerateImagesAsync(1).ConfigureAwait(false);
            imageRefCache.AddOrUpdate(CreateImageRef(generatedImages.First().Value.First()));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(item =>
            {
                list.Add(item);
                Logger.Verbose("File discovered '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[]{ImagesPath});
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            await GenerateImagesAsync(1).ConfigureAwait(false);

            WaitOne(45);

            list.Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldDetectFileDelete()
        {
            var generatedImages = await GenerateImagesAsync(1, ImagesPath).ConfigureAwait(false);
            var path = generatedImages.First().Value.First();

            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            imageRefCache.AddOrUpdate(CreateImageRef(path));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDeleted.Subscribe(item =>
            {
                list.Add(item);
                Logger.Verbose("File deleted '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[]{ImagesPath});
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            Logger.Verbose("Delete Path '{Path}'", path);
            FileSystem.File.Delete(path);

            WaitOne(45);

            list.Should().HaveCount(1);
            list.First().Should().Be(path);
        }

        [Fact]
        public async Task ShouldIgnoreUnknownFileDelete()
        {
            var generatedImages = await GenerateImagesAsync(1, ImagesPath).ConfigureAwait(false);
            var path = generatedImages.First().Value.First();

            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDeleted.Subscribe(item =>
            {
                list.Add(item);
                Logger.Verbose("File deleted '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[]{ImagesPath});
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            Logger.Verbose("Delete Path '{Path}'", path);
            FileSystem.File.Delete(path);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(5)).Should().BeFalse();

            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldDetectFileRename()
        {
            var generatedImages = await GenerateImagesAsync(1, ImagesPath)
                .ConfigureAwait(false);

            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            var imagePath = generatedImages.First().Value.First();
            imageRefCache.AddOrUpdate(CreateImageRef(imagePath));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<(string oldFullPath, string fullPath)>();
            imageContainerWatcherService.FileRenamed.Subscribe(tuple =>
            {
                list.Add(tuple);
                Logger.Verbose("File renamed '{From}' '{To}'", tuple.oldFullPath, tuple.fullPath);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[]{ImagesPath});
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            var movedTo = Path.Combine(generatedImages.First().Key, "a" + FileSystem.Path.GetFileName(imagePath));
            FileSystem.File.Move(imagePath, movedTo);

            Logger.Verbose("Moving File '{Path}' '{ToPath}'", imagePath, movedTo);

            WaitOne(45);

            list.Should().HaveCount(1);
            list.First().Should().Be((imagePath, movedTo));
        }

        [Fact]
        public async Task ShouldDetectUnknownFileRenameAsDiscover()
        {
            var generatedImages = await GenerateImagesAsync(1, ImagesPath).ConfigureAwait(false);
            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var list = new List<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(item =>
            {
                list.Add(item);
                Logger.Verbose("File discovered '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[]{ImagesPath});
            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(3));

            var file = generatedImages.First().Value.First();
            var movedTo = Path.Combine(generatedImages.First().Key, "a" + FileSystem.Path.GetFileName(file));
            FileSystem.File.Move(file, movedTo);

            WaitOne(5);
            list.Should().HaveCount(1);
            list.First().Should().Be(movedTo);
        }
    }
}