using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using Autofac.Extras.NSubstitute;
using FluentAssertions;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Scheduling;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageLocationServiceTests : TestsBase, IDisposable
    {
        private readonly AutoSubstitute _autoSub;
        private readonly AutoResetEvent _autoResetEvent;
        private readonly MockFileSystem _mockFileSystem;

        public ImageLocationServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _autoSub = new AutoSubstitute();
            _autoResetEvent = new AutoResetEvent(false);


            _mockFileSystem = new MockFileSystem();
            _autoSub.Provide<IFileSystem>(_mockFileSystem);
        }

        public void Dispose()
        {
            _autoSub.Dispose();
            _autoResetEvent.Dispose();
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

            foreach (var file in files.Concat(otherFiles))
            {
                _mockFileSystem.AddFile(file, new MockFileData(new byte[0]));
            }

            string[] imagePaths = null;

            var testSchedulerProvider = new TestSchedulerProvider();
            _autoSub.Provide<ISchedulerProvider>(testSchedulerProvider);

            var imageLocationService = _autoSub.Resolve<ImageLocationService>();
            imageLocationService.GetImages(directory)
                .Subscribe(paths =>
                {
                    imagePaths = paths;
                    _autoResetEvent.Set();
                });

            testSchedulerProvider.TaskPool.AdvanceBy(1);
            _autoResetEvent.WaitOne();

            imagePaths.Should().NotBeNull();
            imagePaths.Select(fileInfo => fileInfo)
                .Should().BeEquivalentTo(files);
        }
    }
}