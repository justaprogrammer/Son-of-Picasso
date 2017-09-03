using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PicasaReboot.Core;

namespace PicasaReboot.Tests
{
    [TestClass]
    public class ImageFileSystemServiceTests
    {
        [TestMethod]
        public void ListEmptyFolder()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"c:\images");

            var imageFileSystemService = new ImageFileSystemService(mockFileSystem);
            var items = imageFileSystemService.ListFiles(@"c:\images");

            items.ShouldAllBeEquivalentTo(Enumerable.Empty<string>());
        }

        [TestMethod]
        public void ListFolder()
        {
            var image1Bytes = Resources.image1.ImageToBytes();

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
