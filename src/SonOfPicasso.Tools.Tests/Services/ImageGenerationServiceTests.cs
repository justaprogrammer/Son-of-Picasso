using System;
using System.Drawing;
using System.IO;
using System.Linq;
using ExifLibrary;
using FluentAssertions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Tools.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Tools.Tests.Services
{
    public class ImageGenerationServiceTests : UnitTestsBase
    {
        public ImageGenerationServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void ShouldCopyExifDataToImageFile()
        {
            var exifData = Fakers.ExifDataFaker.Generate();

            ImageFile imageFile;
            using (var manifestResourceStream =
                typeof(TestsBase).Assembly.GetManifestResourceStream(
                    "SonOfPicasso.Testing.Common.Resources.DSC04085.JPG"))
            {
                imageFile = ImageFile.FromStream(manifestResourceStream);
            }

            imageFile.Properties.Clear();

            var imageGenerationService = AutoSubstitute.Resolve<ImageGenerationService>();
            imageGenerationService.CopyExifDataToImageFile(exifData, imageFile);

            var outputStream = new MemoryStream();
            imageFile.Save(outputStream);
        }

        [Fact]
        public void ShouldGenerateImage()
        {
            var imageFolder = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(imageFolder);

            var imageFile = MockFileSystem.Path.Combine(imageFolder, Faker.System.FileName("jpg"));

            var exifData = Fakers.ExifDataFaker.Generate();

            var imageGenerationService = AutoSubstitute.Resolve<ImageGenerationService>();

            imageGenerationService.GenerateImage(imageFile, exifData)
                .Subscribe(s => { AutoResetEvent.Set(); });

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();
         
            using var memoryStream = new MemoryStream(MockFileSystem.GetFile(imageFile).Contents);
            using var image = Image.FromStream(memoryStream);
            image.Size.Width.Should().Be(400);
            image.Size.Height.Should().Be(300);
        }
  
        [Fact]
        public void ShouldGenerateImages()
        {
            var imageFolder = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(imageFolder);

            var imageGenerationService = AutoSubstitute.Resolve<ImageGenerationService>();

            imageGenerationService.GenerateImages(5, imageFolder)
                .Subscribe(s => { }, () => { AutoResetEvent.Set(); });

            TestSchedulerProvider.TaskPool.AdvanceBy(6);

            WaitOne();

            MockFileSystem.Directory.EnumerateFiles(imageFolder, "*.*", SearchOption.AllDirectories).ToArray().Should().HaveCount(5);
        }
    }
}