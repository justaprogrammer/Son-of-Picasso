using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using SonOfPicasso.Core;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Tests.Scheduling;

namespace SonOfPicasso.Tests.Core
{
    [TestFixture]
    public class ImageServiceTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<ImageServiceTests>();

        [Test]
        public void ListFilesOfEmptyFolder()
        {
            Log.Verbose("ListFilesOfEmptyFolder");

            var schedulers = new TestSchedulers();
            var mockFileSystem = MockFileSystemFactory.Create(false);

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var items = imageFileSystemService.ListFiles(MockFileSystemFactory.ImagesFolder);

            items.ShouldAllBeEquivalentTo(Enumerable.Empty<string>());
        }

        [Test]
        public void ListFiles()
        {
            Log.Verbose("ListFiles");

            var schedulers = new TestSchedulers();
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var items = imageFileSystemService.ListFiles(MockFileSystemFactory.ImagesFolder);

            items.ShouldAllBeEquivalentTo(new[] { MockFileSystemFactory.Image1Jpg });

            Log.Debug("Completed");
        }

        [Test]
        public void ListFilesAsync()
        {
            Log.Verbose("ListFilesAsync");

            var schedulers = new TestSchedulers();
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var observable = imageFileSystemService.ListFilesAsync(MockFileSystemFactory.ImagesFolder);

            Log.Debug("Created Observable");

            var autoResetEvent = new AutoResetEvent(false);

            string[] items = null;
            observable
                .Subscribe(strings =>
                {
                    items = strings;
                    autoResetEvent.Set();
                }, () =>
                {
                });

            schedulers.ThreadPool.AdvanceBy(1);
            autoResetEvent.WaitOne();

            items.ShouldAllBeEquivalentTo(new[] { MockFileSystemFactory.Image1Jpg });

            Log.Debug("Completed");
        }

        [Test]
        public void LoadImage()
        {
            Log.Verbose("LoadImage");

            var schedulers = new TestSchedulers();
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var bitmapImage = imageFileSystemService.LoadImage(MockFileSystemFactory.Image1Jpg);
            bitmapImage.Should().NotBeNull();

            Log.Debug("Completed");
        }

        [Test]
        public void LoadImageAsync()
        {
            Log.Verbose("LoadImageAsync");

            var schedulers = new TestSchedulers();
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var observable = imageFileSystemService.LoadImageAsync(MockFileSystemFactory.Image1Jpg);

            Log.Debug("Created Observable");

            var autoResetEvent = new AutoResetEvent(false);

            BitmapImage image = null;
            observable
                .Subscribe(i =>
                {
                    image = i;
                    autoResetEvent.Set();
                }, () =>
                {
                });

            schedulers.ThreadPool.AdvanceBy(1);
            autoResetEvent.WaitOne();

            image.Should().NotBeNull();

            Log.Debug("Completed");
        }
    }
}
