using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{    
    public class ImageLocationServiceTests : UnitTestsBase
    {
        public ImageLocationServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Timeout = 1000)]
        public void CanGetImages()
        {
            Logger.Debug("CanGetImages");
            var directory = Faker.System.DirectoryPathWindows();

            var subDirectory = Path.Combine(directory, Faker.Random.Word());

            var files = new[] {"jpg", "jpeg", "png", "tiff", "tif", "bmp"}
                .Select(ext => Path.Combine(subDirectory, Faker.System.FileName(ext)))
                .ToArray();

            var otherFiles = new[] {"txt", "doc"}
                .Select(ext => Path.Combine(subDirectory, Faker.System.FileName(ext)))
                .ToArray();

            foreach (var file in files.Concat(otherFiles)) MockFileSystem.AddFile(file, new MockFileData(new byte[0]));

            IFileInfo[] imagePaths = null;

            var imageLocationService = AutoSubstitute.Resolve<ImageLocationService>();
            imageLocationService.GetImages(directory)
                .ToArray()
                .Subscribe(paths =>
                {
                    imagePaths = paths;
                    AutoResetEvent.Set();
                });

            TestSchedulerProvider.TaskPool.AdvanceBy(1);
            AutoResetEvent.WaitOne();

            imagePaths.Should().NotBeNull();
            imagePaths.Select(fileInfo => fileInfo.FullName)
                .Should().BeEquivalentTo(files);
        }
    }
}