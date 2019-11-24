using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autofac;
using FluentAssertions;
using SixLabors.ImageSharp;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.UI.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Integration.Tests.Services
{
    public class ImageLoadingServiceIntegrationTests : IntegrationTestsBase
    {
        public ImageLoadingServiceIntegrationTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            var containerBuilder = GetContainerBuilder();

            containerBuilder.RegisterType<TestBlobCacheProvider>()
                .As<IBlobCacheProvider>();

            containerBuilder.RegisterType<ImageLoadingService>();

            Container = containerBuilder.Build();
        }

        protected override IContainer Container { get; }

        [Fact]
        public async Task ShouldLoadAndCacheThumbnail()
        {
            var generateImages = await GenerateImagesAsync(1, ImagesPath);
            var imagePath = generateImages.First().Value.First();

            var cacheDirectoryInfo = ImagesDirectoryInfo.CreateSubdirectory("Cache");

            var list = new List<Image>();

            var imageLoadingService = Container.Resolve<ImageLoadingService>();
            imageLoadingService.LoadThumbnailFromPathInternal(imagePath, cacheDirectoryInfo.FullName, observeOnlyThumbnail: false)
                .Subscribe(source =>
                {
                    list.Add(source);
                }, () =>
                {
                    Set();
                });

            WaitOne(5);

            list.Should().HaveCount(2);
            list[1].Width.Should().BeLessOrEqualTo(300);
            list[1].Height.Should().BeLessOrEqualTo(300);
            
            var cachedFiles = cacheDirectoryInfo.GetFiles();
            cachedFiles.Should().HaveCount(1);

            list.Clear();

            imageLoadingService.LoadThumbnailFromPathInternal(imagePath, cacheDirectoryInfo.FullName, observeOnlyThumbnail: false)
                .Subscribe(source =>
                {
                    list.Add(source);
                }, () =>
                {
                    Set();
                });

            WaitOne(5);

            list.Should().HaveCount(1);
            list[0].Width.Should().BeLessOrEqualTo(300);
            list[0].Height.Should().BeLessOrEqualTo(300);

            cachedFiles.First().Delete();

            list.Clear();
        
            imageLoadingService.LoadThumbnailFromPathInternal(imagePath, cacheDirectoryInfo.FullName, observeOnlyThumbnail: false)
                .Subscribe(source =>
                {
                    list.Add(source);
                }, () =>
                {
                    Set();
                });

            WaitOne(5);

            list.Should().HaveCount(2);
            list[1].Width.Should().BeLessOrEqualTo(300);
            list[1].Height.Should().BeLessOrEqualTo(300);
        } 
    } 
}