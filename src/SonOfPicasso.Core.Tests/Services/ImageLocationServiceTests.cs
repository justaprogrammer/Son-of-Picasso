using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using FluentAssertions;
using MoreLinq;
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
            var imageLocationService = this.CreateImageLocationService();
        }

        [Fact(Timeout = 1000)]
        public void CanGetImages()
        {
            Logger.Debug("CanGetImages");

            var directory = Faker.System.DirectoryPathWindows();

            var subDirectory = Path.Join(directory, Faker.Random.Word());

            var mockFileSystem = new MockFileSystem();

            var files = new[] { "jpg", "jpeg", "png", "tiff", "tif", "bmp" }
                .Select(ext => Path.Join(subDirectory, Faker.System.FileName(ext)))
                .ToArray();

            var otherFiles = new[] { "txt", "doc" }
                .Select(ext => Path.Join(subDirectory, Faker.System.FileName(ext)))
                .ToArray();

            foreach (var file in files.Concat(otherFiles))
            {
                mockFileSystem.AddFile(file, new MockFileData(new byte[0]));
            }

            var autoResetEvent = new AutoResetEvent(false);

            string[] imagePaths = null;

            var testSchedulerProvider = new TestSchedulerProvider();

            var imageLocationService = this.CreateImageLocationService(mockFileSystem, testSchedulerProvider);
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