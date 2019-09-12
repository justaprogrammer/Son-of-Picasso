using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Threading;
using Autofac;
using Autofac.Extras.NSubstitute;
using AutofacSerilogIntegration;
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
        private readonly TestSchedulerProvider _testSchedulerProvider;

        public ImageLoadingServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            var builder = new ContainerBuilder();
            builder.RegisterLogger();

            _testSchedulerProvider = new TestSchedulerProvider();
            builder.RegisterInstance(_testSchedulerProvider).As<ISchedulerProvider>();

            _mockFileSystem = new MockFileSystem();
            builder.RegisterInstance(_mockFileSystem).As<IFileSystem>();

            _autoSubstitute = new AutoSubstitute(builder);
            _autoResetEvent = new AutoResetEvent(false);
        }

        public void Dispose()
        {
            _autoSubstitute.Dispose();
            _autoResetEvent.Dispose();
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

            var imageLoadingService = _autoSubstitute.Resolve<ImageLoadingService>();

            imageLoadingService.LoadImageFromPath(filePath).Subscribe(b =>
            {
                bitmap = b;
                _autoResetEvent.Set();
            });

            _testSchedulerProvider.TaskPool.AdvanceBy(1);

            _autoResetEvent.WaitOne();

            bitmap.Should().NotBeNull();
            bitmap.Height.Should().BeApproximately(1000.6f, 0.01f);
            bitmap.Width.Should().BeApproximately(1334.12f, 0.01f);
        }
    }
}