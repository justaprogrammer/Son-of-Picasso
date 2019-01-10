using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using SonOfPicasso.Core.Extensions;
using SonOfPicasso.SampleImages;

namespace SonOfPicasso.Tests
{
    public class MockFileSystemFactory
    {
        public static readonly string ImagesFolder = @"c:\images";
        public static readonly byte[] Image1Bytes = Resources.image1.GetBytes();
        public static readonly string Image1Jpg = $@"{ImagesFolder}\image1.jpg";

        static MockFileSystemFactory()
        {
        }

        public static IFileSystem Create(bool addFiles = true)
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(ImagesFolder);

            if (addFiles)
            {
                mockFileSystem.AddFile(Image1Jpg, new MockFileData(Image1Bytes));
            }

            return mockFileSystem;
        }
    }
}