using System;
using System.Threading;
using System.Windows.Media.Imaging;
using SonOfPicasso.Core;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Tests;
using SonOfPicasso.Tests.Scheduling;
using SonOfPicasso.Windows.ViewModels;

namespace SonOfPicasso.Windows.Tests
{
    [TestFixture]
    public class ImageViewModelTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<ImageViewModelTests>();

        [Test]
        public void CanCreateImageViewModel()
        {
            Log.Verbose("CanCreateImageViewModel");

            var schedulers = new TestSchedulers();
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var imageViewModel = new ImageViewModel(imageFileSystemService, MockFileSystemFactory.Image1Jpg, schedulers);

            imageViewModel.File.Should().Be(MockFileSystemFactory.Image1Jpg);

            var autoResetEvent = new AutoResetEvent(false);

            BitmapImage bitmapImage = null;

            imageViewModel.Image
                .Subscribe(image => {
                    Log.Verbose("Loaded");
                    bitmapImage = image;
                    autoResetEvent.Set();
                });

            schedulers.ThreadPool.AdvanceBy(1);

            autoResetEvent.WaitOne(TimeSpan.FromSeconds(1)).Should().BeTrue();
            bitmapImage.Should().NotBeNull();
        }
    }
}