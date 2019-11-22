using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using FluentAssertions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.UI.Services;
using Splat;
using Splat.Serilog;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.Services
{
    public class ImageLoadingServiceTests : UnitTestsBase
    {
        public ImageLoadingServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            Locator.CurrentMutable.RegisterPlatformBitmapLoader();
            Locator.CurrentMutable.UseSerilogFullLogger();
        }

        [Fact]
        public void CanLoadImage()
        {
            Logger.Debug("CanLoadImage");

            var resourceAssembly = Assembly.GetAssembly(typeof(TestsBase));

            var filePath = Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("jpg"));
            MockFileSystem.AddFileFromEmbeddedResource(filePath, resourceAssembly,
                "SonOfPicasso.Testing.Common.Resources.DSC04085.JPG");

            BitmapSource bitmap = null;

            var imageLoadingService = AutoSubstitute.Resolve<ImageLoadingService>();

            imageLoadingService.LoadImageFromPath(filePath).Subscribe(b =>
            {
                bitmap = b;
                AutoResetEvent.Set();
            });

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            AutoResetEvent.WaitOne();

            bitmap.Should().NotBeNull();
            bitmap.Height.Should().BeApproximately(1000.6f, 0.01f);
            bitmap.Width.Should().BeApproximately(1334.12f, 0.01f);
        }
    }
}