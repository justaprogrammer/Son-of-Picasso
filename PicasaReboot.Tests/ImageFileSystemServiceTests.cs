using System;
using System.Drawing;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
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

            items.ShouldAllBeEquivalentTo(Enumerable.Empty<ImageFile>());
        }

        [TestMethod]
        public void ListFolder()
        {
            var image1Bytes = Resources.image1.ImageToBytes();

            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"c:\images");
            mockFileSystem.AddFile(@"c:\images\image1.jpg", new MockFileData(image1Bytes));

            var imageFileSystemService = new ImageFileSystemService(mockFileSystem);
            var items = imageFileSystemService.ListFiles(@"c:\images");

            items.ShouldAllBeEquivalentTo(new [] {new ImageFile(@"c:\images\image1.jpg"), });
        }
    }
}
