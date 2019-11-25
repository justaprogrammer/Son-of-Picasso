using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Serilog;
using SixLabors.ImageSharp;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common.Scheduling;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class ImageLoadingServiceIntegrationTests : IntegrationTestsBase
    {
        public ImageLoadingServiceIntegrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            ThumbnailsDirectoryInfo = ImagesDirectoryInfo.Parent.CreateSubdirectory("Thumbnails");

            var containerBuilder = GetContainerBuilder();

            containerBuilder.RegisterType<TestBlobCacheProvider>()
                .As<IBlobCacheProvider>();

            containerBuilder.Register(context =>
            {
                var logger = context.Resolve<ILogger>().ForContext<ImageLoadingService>();

                return new ImageLoadingService(context.Resolve<IFileSystem>(), logger,
                    context.Resolve<ISchedulerProvider>(), context.Resolve<IBlobCacheProvider>(),
                    ThumbnailsDirectoryInfo.FullName);
            }).As<ImageLoadingService>();

            Container = containerBuilder.Build();
        }

        public IDirectoryInfo ThumbnailsDirectoryInfo { get; set; }

        protected override IContainer Container { get; }

        [Fact]
        public async Task ShouldLoadAndCacheThumbnail()
        {
            var generateImages = await GenerateImagesAsync(1, ImagesPath);
            var imagePath = generateImages.First().Value.First();

            var cacheDirectoryInfo = ImagesDirectoryInfo.CreateSubdirectory("Cache");

            var list = new List<Image>();

            var imageLoadingService = Container.Resolve<ImageLoadingService>();
            imageLoadingService
                .LoadThumbnailFromPathInternal(imagePath, observeOnlyThumbnail: false)
                .Subscribe(source => { list.Add(source); }, () => { Set(); });

            WaitOne(15);

            list.Should().HaveCount(2);
            list[1].Width.Should().BeLessOrEqualTo(300);
            list[1].Height.Should().BeLessOrEqualTo(300);

            var cachedFiles = cacheDirectoryInfo.GetFiles();
            cachedFiles.Should().HaveCount(1);

            list.Clear();

            imageLoadingService
                .LoadThumbnailFromPathInternal(imagePath, observeOnlyThumbnail: false)
                .Subscribe(source => { list.Add(source); }, () => { Set(); });

            WaitOne(15);

            list.Should().HaveCount(1);
            list[0].Width.Should().BeLessOrEqualTo(300);
            list[0].Height.Should().BeLessOrEqualTo(300);

            cachedFiles.First().Delete();

            list.Clear();

            imageLoadingService
                .LoadThumbnailFromPathInternal(imagePath, observeOnlyThumbnail: false)
                .Subscribe(source => { list.Add(source); }, () => { Set(); });

            WaitOne(15);

            list.Should().HaveCount(2);
            list[1].Width.Should().BeLessOrEqualTo(300);
            list[1].Height.Should().BeLessOrEqualTo(300);
        }
    }
}