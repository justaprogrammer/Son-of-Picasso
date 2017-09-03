using System.IO.Abstractions.TestingHelpers;
using System.Linq;
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

            var imageFileSystemService = new ImageFileSystemService(mockFileSystem);
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

            var imageFileSystemService = new ImageFileSystemService(mockFileSystem);
            var items = imageFileSystemService.ListFiles(@"c:\images");

            items.ShouldAllBeEquivalentTo(new [] { image1Jpg });
        }
    }
}
