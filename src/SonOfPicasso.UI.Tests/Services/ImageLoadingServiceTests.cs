using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.UI.Tests.Extensions;
using Splat;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.Services
{
    public class ImageLoadingServiceTests : TestsBase<ImageLoadingServiceTests>
    {
        public ImageLoadingServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.Debug("CanInitialize");

            var imageLoadingService = this.CreateImageLoadingService();
        }

        [Fact(Timeout = 500)]
        public void CanLoadImage()
        {
            Logger.Debug("CanLoadImage");

            var mockFileSystem = new MockFileSystem();
            var resourceAssembly = Assembly.GetAssembly(typeof(ImageLoadingServiceTests));

            var filePath = Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("jpg"));
            mockFileSystem.AddFileFromEmbeddedResource(filePath, resourceAssembly, "SonOfPicasso.UI.Tests.Resources.DSC04085.JPG");

            var autoResetEvent = new AutoResetEvent(false);

            IBitmap bitmap = null;

            var testSchedulerProvider = new TestSchedulerProvider();

            var imageLoadingService = this.CreateImageLoadingService(mockFileSystem, testSchedulerProvider);
            imageLoadingService.LoadImageFromPath(filePath).Subscribe(b =>
            {
                bitmap = b;
                autoResetEvent.Set();
            });

            testSchedulerProvider.TaskPool.AdvanceBy(1);

            autoResetEvent.WaitOne();

            bitmap.Should().NotBeNull();
            bitmap.Height.Should().BeApproximately(1000.6f, 0.01f);
            bitmap.Width.Should().BeApproximately(1334.12f, 0.01f);
        }
    }
}