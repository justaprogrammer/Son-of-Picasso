using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AutoBogus;
using Bogus;
using ExifLibrary;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common.Extensions;

namespace SonOfPicasso.Testing.Common
{
    public static class Fakers
    {
        public static Faker<ExifData> ExifDataFaker => LazyExifDataFaker.Value;
        private static readonly Lazy<Faker<ExifData>> LazyExifDataFaker = new Lazy<Faker<ExifData>>(() => 
            new AutoFaker<ExifData>()
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
                .RuleFor(data => data.ThumbnailYCbCrPositioning,
                    faker => faker.PickRandom<YCbCrPositioning>().ToString())
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
                .RuleFor(data => data.LensSpecification,
                    faker =>
                        $"{faker.Random.UInt()}/{faker.Random.UInt()} F{faker.Random.UInt()}/{faker.Random.UInt()}, {faker.Random.UInt()}/{faker.Random.UInt()} F{faker.Random.UInt()}/{faker.Random.UInt()}")
                .RuleFor(data => data.ExifVersion, faker => faker.Random.Short().ToString())
                .RuleFor(data => data.FlashpixVersion, faker => faker.Random.Short().ToString())
                .RuleFor(data => data.InteroperabilityVersion, faker => faker.Random.Short().ToString()));

        public static Faker<Folder> FolderFaker => LazyFolderFaker.Value;
        private static readonly  Lazy<Faker<Folder>> LazyFolderFaker = new Lazy<Faker<Folder>>(() => 
            new AutoFaker<Folder>()
                .RuleFor(folder => folder.Images, () => new List<Image>())
                .RuleFor(folder => folder.Path, faker => faker.System.DirectoryPathWindows()));

        public static Faker<Image> ImageFaker => LazyImageFaker.Value;
        private static readonly Lazy<Faker<Image>> LazyImageFaker = new Lazy<Faker<Image>>(() => 
            new AutoFaker<Image>()
                .RuleFor(image => image.ExifData, ExifDataFaker)
                .RuleFor(image => image.Folder, FolderFaker)
                .RuleFor(image => image.AlbumImages, () => new List<AlbumImage>())
                .FinishWith((faker, image) =>
                {
                    image.Path = Path.Join(faker.System.DirectoryPathWindows(), faker.System.FileName("jpg"));
                    image.ExifDataId = image.ExifData.Id;
                    image.FolderId = image.Folder.Id;
                    image.Folder.Images.Add(image);
                }));
    }
}
