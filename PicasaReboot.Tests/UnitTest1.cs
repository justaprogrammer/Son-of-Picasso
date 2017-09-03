using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PicasaReboot.Core;

namespace PicasaReboot.Tests
{
    [TestClass]
    public class ImageFileSystemServiceTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var imageFileSystemService = new ImageFileSystemService(new MockFileSystem());
        }
    }
}
