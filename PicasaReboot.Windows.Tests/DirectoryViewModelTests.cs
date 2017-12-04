using System.Threading;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using PicasaReboot.Tests;
using PicasaReboot.Tests.Scheduling;
using PicasaReboot.Windows.ViewModels;
using Serilog;

namespace PicasaReboot.Windows.Tests
{

    [TestFixture]
    public class DirectoryViewModelTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<DirectoryViewModelTests>();

        [Test]
        public void CanCreateDirectoryViewModel()
        {
            Log.Verbose("CanCreateDirectoryViewModel");

            var schedulers = new TestSchedulers();
            schedulers.ThreadPool.Start();
            schedulers.Dispatcher.Start();

            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var directoryViewModel = new DirectoryViewModel(imageFileSystemService, MockFileSystemFactory.ImagesFolder, schedulers);

            var autoResetEvent = new AutoResetEvent(false);
        }
    }
}
