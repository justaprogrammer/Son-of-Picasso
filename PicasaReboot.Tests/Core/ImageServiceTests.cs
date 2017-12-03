using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using PicasaReboot.Tests.Scheduling;
using Serilog;

namespace PicasaReboot.Tests.Core
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
            var items = imageFileSystemService.ListFiles(@"c:\images");

            items.ShouldAllBeEquivalentTo(Enumerable.Empty<string>());
        }

        [Test]
        public void ListFiles()
        {
            Log.Verbose("ListFiles");

            var schedulers = new TestSchedulers();
            var mockFileSystem = MockFileSystemFactory.Create();

            var imageFileSystemService = new ImageService(mockFileSystem, schedulers);
            var items = imageFileSystemService.ListFiles(@"c:\images");

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
            var observable = imageFileSystemService.ListFilesAsync(@"c:\images");

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
        }
    }
}
