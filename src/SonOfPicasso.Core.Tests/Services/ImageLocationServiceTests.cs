using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Extras.NSubstitute;
using FluentAssertions;
using MoreLinq;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Scheduling;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageLocationServiceTests : TestsBase<ImageLocationServiceTests>
    {
        public ImageLocationServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.Debug("CanInitialize");
            using (var autoSub = new AutoSubstitute())
            {
                var imageLocationService = autoSub.Resolve<ImageLocationService>();
            }
        }

        [Fact(Timeout = 1000)]
        public void CanGetImages()
        {
            Logger.Debug("CanGetImages");
            using (var autoSub = new AutoSubstitute())
            {
                var directory = Faker.System.DirectoryPathWindows();

                var subDirectory = Path.Combine(directory, Faker.Random.Word());

                var mockFileSystem = new MockFileSystem();
                autoSub.Provide<IFileSystem>(mockFileSystem);

                var files = new[] { "jpg", "jpeg", "png", "tiff", "tif", "bmp" }
                    .Select(ext => Path.Combine(subDirectory, Faker.System.FileName(ext)))
                    .ToArray();

                var otherFiles = new[] { "txt", "doc" }
                    .Select(ext => Path.Combine(subDirectory, Faker.System.FileName(ext)))
                    .ToArray();

                foreach (var file in files.Concat(otherFiles))
                {
                    mockFileSystem.AddFile(file, new MockFileData(new byte[0]));
                }

                var autoResetEvent = new AutoResetEvent(false);

                string[] imagePaths = null;

                var testSchedulerProvider = new TestSchedulerProvider();
                autoSub.Provide<ISchedulerProvider>(testSchedulerProvider);

                var imageLocationService = autoSub.Resolve<ImageLocationService>();
                imageLocationService.GetImages(directory)
                    .Subscribe(paths =>
                    {
                        imagePaths = paths;
                        autoResetEvent.Set();
                    });

                testSchedulerProvider.TaskPool.AdvanceBy(1);
                autoResetEvent.WaitOne();

                imagePaths.Should().NotBeNull();
                imagePaths.Select(fileInfo => fileInfo)
                    .Should().BeEquivalentTo(files);
            }
        }
    }
}