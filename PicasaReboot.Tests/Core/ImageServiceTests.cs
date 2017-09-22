using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using Serilog;

namespace PicasaReboot.Tests.Core
{
    [TestFixture]
    public class ImageServiceTests
    {
        private static ILogger Log { get; } = LogManager.ForContext<ImageServiceTests>();

        [Test]
        public void ListEmptyFolder()
        {
            Log.Verbose("TestDirectory {TestDirectory}", TestContext.CurrentContext.TestDirectory);

            var mockFileSystem = MockFileSystemFactory.Create(false);

            var imageFileSystemService = new ImageService(mockFileSystem);
            var items = imageFileSystemService.ListFiles(@"c:\images");

            items.ShouldAllBeEquivalentTo(Enumerable.Empty<string>());
        }

        [Test]
        public void ListFolder()
        {
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem);
            var items = imageFileSystemService.ListFiles(@"c:\images");

            items.ShouldAllBeEquivalentTo(new[] { MockFileSystemFactory.Image1Jpg });

            Log.Debug("Completed");
        }

        [Test]
        public void ListFolderAsync()
        {
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem);
            var observable = imageFileSystemService.ListFilesAsync(@"c:\images");

            var autoResetEvent = new AutoResetEvent(false);

            string[] items = null;
            observable.Subscribe(strings =>
            {
                items = strings;
            }, () =>
            {
                autoResetEvent.Set();
            });

            autoResetEvent.WaitOne();
            items.ShouldAllBeEquivalentTo(new[] { MockFileSystemFactory.Image1Jpg });
        }
    }
}
