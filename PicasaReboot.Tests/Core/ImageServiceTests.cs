using System;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using PicasaReboot.Core;
using PicasaReboot.Core.Extensions;
using PicasaReboot.SampleImages;

namespace PicasaReboot.Tests.Core
{
    [TestFixture]
    public class ImageServiceTests
    {
        [Test]
        public void ListEmptyFolder()
        {
            Log.Verbose(Assembly.GetExecutingAssembly().Location);
            Log.Verbose(Environment.CurrentDirectory);
            Log.Verbose("TestDirectory {TestDirectory}", TestContext.CurrentContext.TestDirectory);

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
            items.ShouldAllBeEquivalentTo(new[] { image1Jpg });
        }
    }
}
