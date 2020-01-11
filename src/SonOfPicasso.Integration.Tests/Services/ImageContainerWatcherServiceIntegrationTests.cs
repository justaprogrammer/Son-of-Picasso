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

            imageContainerWatcherService.Start(imageRefCache, new[] { ImagesPath });

            var generatedImages = await GenerateImagesAsync(1).ConfigureAwait(false);
            var path = generatedImages.First().Value.First();

            WaitOne(15);

            using (new AssertionScope())
            {
                set.Should().HaveCount(1);
                set.First().Should().Be(path);
            }

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

            imageContainerWatcherService.Start(imageRefCache, new[] { ImagesPath });

            var generatedImages = await GenerateImagesAsync(1).ConfigureAwait(false);
            var path = generatedImages.First().Value.First();

            WaitOne(15);

            using (new AssertionScope())
            {
                set.Should().HaveCount(1);
                set.First().Should().Be(path);
            }

            await ImageGenerationService.GenerateImage(path,
                Fakers.ExifDataFaker);

            WaitOne(15);

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
            var path = generatedImages.First().Value.First();

            imageRefCache.AddOrUpdate(CreateImageRef(generatedImages.First().Value.First()));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var set = new HashSet<string>();
            imageContainerWatcherService.FileDiscovered.Subscribe(item =>
            {
                set.Add(item);
                Logger.Verbose("File discovered '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[] { ImagesPath });

            generatedImages = await GenerateImagesAsync(1).ConfigureAwait(false);
            path = generatedImages.First().Value.First();

            WaitOne(15);

            using (new AssertionScope())
            {
                set.Should().HaveCount(1);
                set.First().Should().Be(path);
            }
        }

        [Fact]
        public async Task ShouldDetectFileDelete()
        {
            var generatedImages = await GenerateImagesAsync(1, ImagesPath).ConfigureAwait(false);
            var path = generatedImages.First().Value.First();

            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);
            imageRefCache.AddOrUpdate(CreateImageRef(path));

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var set = new HashSet<string>();
            imageContainerWatcherService.FileDeleted.Subscribe(item =>
            {
                set.Add(item);
                Logger.Verbose("File deleted '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[] { ImagesPath });

            Logger.Verbose("Delete Path '{Path}'", path);
            FileSystem.File.Delete(path);

            WaitOne(15);

            using (new AssertionScope())
            {
                set.Should().HaveCount(1);
                set.First().Should().Be(path);
            }
        }

        [Fact]
        public async Task ShouldIgnoreUnknownFileDelete()
        {
            var generatedImages = await GenerateImagesAsync(1, ImagesPath).ConfigureAwait(false);
            var path = generatedImages.First().Value.First();

            using var imageRefCache = new SourceCache<ImageRef, string>(imageRef => imageRef.ImagePath);

            var imageContainerWatcherService = Container.Resolve<ImageContainerWatcherService>();

            var set = new HashSet<string>();
            imageContainerWatcherService.FileDeleted.Subscribe(item =>
            {
                set.Add(item);
                Logger.Verbose("File deleted '{Item}'", item);
                Set();
            });

            imageContainerWatcherService.Start(imageRefCache, new[] { ImagesPath });

            Logger.Verbose("Delete Path '{Path}'", path);
            FileSystem.File.Delete(path);

            AutoResetEvent.WaitOne(TimeSpan.FromSeconds(5)).Should().BeFalse();

            set.Should().BeEmpty();
        }
    }
}