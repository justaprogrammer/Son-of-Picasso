using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Core.Extensions;
using PicasaReboot.SampleImages;

namespace PicasaReboot.Tests
{
    [TestFixture]
    public class ImageFileSystemServiceTests
    {
        [Test]
        public void ListEmptyFolder()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"c:\images");

            var imageFileSystemService = new ImageService(mockFileSystem);
            var items = imageFileSystemService.ListFiles(@"c:\images");

            items.ShouldAllBeEquivalentTo(Enumerable.Empty<string>());
        }

        [Test]
        public void ListFolder()
        {
            var image1Bytes = Resources.image1.GetBytes();

            var image1Jpg = @"c:\images\image1.jpg";

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"c:\images");
            mockFileSystem.AddFile(image1Jpg, new MockFileData(image1Bytes));

            var imageFileSystemService = new ImageService(mockFileSystem);
            var items = imageFileSystemService.ListFiles(@"c:\images");

            items.ShouldAllBeEquivalentTo(new[] { image1Jpg });

            Log.Debug("Completed");
        }

        [Test]
        public void ListFolderAsync()
        {
            var image1Bytes = Resources.image1.GetBytes();

            var image1Jpg = @"c:\images\image1.jpg";

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"c:\images");
            mockFileSystem.AddFile(image1Jpg, new MockFileData(image1Bytes));

            var imageFileSystemService = new ImageService(mockFileSystem);
            var items = imageFileSystemService.ListFilesAsync(@"c:\images");

            var autoResetEvent = new AutoResetEvent(false);

            var observer = Observer.Create<string[]>(strings =>
            {
                Log.Debug("Blah");
            }, () =>
            {
                Log.Debug("Completed");
                autoResetEvent.Set();
            });

            var observable = items.Repeat();

            observable.SubscribeOn(ThreadPoolScheduler.Instance).Subscribe(observer);
            autoResetEvent.WaitOne();

            observable.SubscribeOn(ThreadPoolScheduler.Instance).Subscribe(observer);
            autoResetEvent.WaitOne();
        }
    }
}
