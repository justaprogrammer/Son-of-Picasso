using System;
using System.IO;
using AutoBogus;
using Bogus;
using ExifLibrary;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Tools.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Tools.Tests.Services
{
    public class ImageGenerationServiceTests : UnitTestsBase
    {
        private readonly Faker<ExifData> _exifDataFaker;

        public ImageGenerationServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _exifDataFaker = new AutoFaker<ExifData>()
                .RuleFor(data => data.FileSource, faker => faker.PickRandom<FileSource>().ToString())
                .RuleFor(data => data.Orientation, faker => faker.PickRandom<Orientation>().ToString())
                .RuleFor(data => data.ColorSpace, faker => faker.PickRandom<ColorSpace>().ToString())
                .RuleFor(data => data.ExposureMode, faker => faker.PickRandom<ExposureMode>().ToString())
                .RuleFor(data => data.MeteringMode, faker => faker.PickRandom<MeteringMode>().ToString())
                .RuleFor(data => data.LightSource, faker => faker.PickRandom<LightSource>().ToString())
                .RuleFor(data => data.SceneCaptureType, faker => faker.PickRandom<SceneCaptureType>().ToString())
                .RuleFor(data => data.ResolutionUnit, faker => faker.PickRandom<ResolutionUnit>().ToString())
                .RuleFor(data => data.YCbCrPositioning, faker => faker.PickRandom<YCbCrPositioning>().ToString())
                .RuleFor(data => data.ExposureProgram, faker => faker.PickRandom<ExposureProgram>().ToString())
                .RuleFor(data => data.Flash, faker => faker.PickRandom<Flash>().ToString())
                .RuleFor(data => data.SceneType, faker => faker.PickRandom<SceneType>().ToString())
                .RuleFor(data => data.CustomRendered, faker => faker.PickRandom<CustomRendered>().ToString())
                .RuleFor(data => data.WhiteBalance, faker => faker.PickRandom<WhiteBalance>().ToString())
                .RuleFor(data => data.Contrast, faker => faker.PickRandom<Contrast>().ToString())
                .RuleFor(data => data.Saturation, faker => faker.PickRandom<Saturation>().ToString())
                .RuleFor(data => data.Sharpness, faker => faker.PickRandom<Sharpness>().ToString())
                .RuleFor(data => data.ThumbnailCompression, faker => faker.PickRandom<Compression>().ToString())
                .RuleFor(data => data.ThumbnailOrientation, faker => faker.PickRandom<Orientation>().ToString())
                .RuleFor(data => data.ThumbnailResolutionUnit, faker => faker.PickRandom<ResolutionUnit>().ToString())
                .RuleFor(data => data.ThumbnailYCbCrPositioning, faker => faker.PickRandom<YCbCrPositioning>().ToString())
                .RuleFor(data => data.XResolution, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.YResolution, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.ThumbnailXResolution, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.ThumbnailYResolution, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.ExposureTime, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.CompressedBitsPerPixel, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.FocalLength, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.FNumber, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.MaxApertureValue, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.DigitalZoomRatio, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.BrightnessValue, faker => $"{faker.Random.Short()}/{faker.Random.Short()}")
                .RuleFor(data => data.ExposureBiasValue, faker => $"{faker.Random.Short()}/{faker.Random.Short()}")
                .RuleFor(data => data.LensSpecification, faker => $"{faker.Random.UInt()}/{faker.Random.UInt()} F{faker.Random.UInt()}/{faker.Random.UInt()}, {faker.Random.UInt()}/{faker.Random.UInt()} F{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.ExifVersion, faker => faker.Random.Short().ToString())
                .RuleFor(data => data.FlashpixVersion, faker => faker.Random.Short().ToString())
                .RuleFor(data => data.InteroperabilityVersion, faker => faker.Random.Short().ToString())
                ;

        }

        [Fact]
        public void ShouldGenerateImage()
        {

            var imageFolder = Faker.System.DirectoryPathWindows();
            MockFileSystem.AddDirectory(imageFolder);

            var imageFile = MockFileSystem.Path.Combine(imageFolder, Faker.System.FileName("jpg"));

            var exifData = _exifDataFaker.Generate();

            var imageGenerationService = AutoSubstitute.Resolve<ImageGenerationService>();

            imageGenerationService.GenerateImage(imageFile, 400, 300, exifData)
                .Subscribe(s =>
                {
                    AutoResetEvent.Set();
                });

            TestSchedulerProvider.TaskPool.AdvanceBy(1);

            WaitOne();
        }

        [Fact]
        public void ShouldCopyExifDataToImageFile()
        {
            var exifData = _exifDataFaker.Generate();
            
            ImageFile imageFile;
            using (var manifestResourceStream = typeof(TestsBase).Assembly.GetManifestResourceStream("SonOfPicasso.Testing.Common.Resources.DSC04085.JPG"))
            {
                imageFile = ImageFile.FromStream(manifestResourceStream);
            }
            imageFile.Properties.Clear();

            var imageGenerationService = AutoSubstitute.Resolve<ImageGenerationService>();
            imageGenerationService.CopyExifDataToImageFile(exifData, imageFile);

            var outputStream = new MemoryStream();
            imageFile.Save(outputStream);
        }
    }
}