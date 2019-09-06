using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Threading;
using Autofac.Extras.NSubstitute;
using FluentAssertions;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Scheduling;
using SonOfPicasso.UI.Services;
using Splat;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.Services
{
    public class ImageLoadingServiceTests : TestsBase
    {
        public ImageLoadingServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Timeout = 500)]
        public void CanLoadImage()
        {
            Logger.Debug("CanLoadImage");
            using (var autoSub = new AutoSubstitute())
            {
                var mockFileSystem = new MockFileSystem();
                autoSub.Provide<IFileSystem>(mockFileSystem);

                var resourceAssembly = Assembly.GetAssembly(typeof(ImageLoadingServiceTests));

                var filePath = Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("jpg"));
                mockFileSystem.AddFileFromEmbeddedResource(filePath, resourceAssembly, "SonOfPicasso.UI.Tests.Resources.DSC04085.JPG");

                var autoResetEvent = new AutoResetEvent(false);

                IBitmap bitmap = null;

                var testSchedulerProvider = new TestSchedulerProvider();
                autoSub.Provide<ISchedulerProvider>(testSchedulerProvider);

                var imageLoadingService = autoSub.Resolve<ImageLoadingService>();

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
}