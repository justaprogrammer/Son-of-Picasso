using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using PicasaReboot.Tests;
using PicasaReboot.Tests.Core;
using PicasaReboot.Tests.Scheduling;
using PicasaReboot.Windows.ViewModels;
using Serilog;

namespace PicasaReboot.Windows.Tests
{
    [TestFixture]
    public class ImageViewModelTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<ImageViewModelTests>();

        [Test]
        public void CanCreateImageViewModel()
        {
            var schedulers = new TestSchedulers();
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var imageViewModel = new ImageViewModel(imageFileSystemService, MockFileSystemFactory.Image1Jpg);

            imageViewModel.File.Should().Be(MockFileSystemFactory.Image1Jpg);
            imageViewModel.Image.Should().NotBeNull();
        }
    }
}