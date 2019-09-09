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
    public class ImageLoadingServiceTests : TestsBase, IDisposable
    {
        private readonly AutoSubstitute _autoSubstitute;
        private readonly MockFileSystem _mockFileSystem;
        private readonly AutoResetEvent _autoResetEvent;

        public ImageLoadingServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _autoSubstitute = new AutoSubstitute();

            _mockFileSystem = new MockFileSystem();
            _autoSubstitute.Provide<IFileSystem>(_mockFileSystem);

            _autoResetEvent = new AutoResetEvent(false);
        }

        public void Dispose()
        {
            _autoSubstitute.Dispose();
        }

        [Fact]
        public void CanLoadImage()
        {
            Logger.Debug("CanLoadImage");

            var resourceAssembly = Assembly.GetAssembly(typeof(TestsBase));

            var filePath = Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("jpg"));
            _mockFileSystem.AddFileFromEmbeddedResource(filePath, resourceAssembly,
                "SonOfPicasso.Testing.Common.Resources.DSC04085.JPG");

            IBitmap bitmap = null;

            var testSchedulerProvider = new TestSchedulerProvider();
            _autoSubstitute.Provide<ISchedulerProvider>(testSchedulerProvider);

            var imageLoadingService = _autoSubstitute.Resolve<ImageLoadingService>();

            imageLoadingService.LoadImageFromPath(filePath).Subscribe(b =>
            {
                bitmap = b;
                _autoResetEvent.Set();
            });

            testSchedulerProvider.TaskPool.AdvanceBy(1);

            _autoResetEvent.WaitOne();

            bitmap.Should().NotBeNull();
            bitmap.Height.Should().BeApproximately(1000.6f, 0.01f);
            bitmap.Width.Should().BeApproximately(1334.12f, 0.01f);
        }
    }
}