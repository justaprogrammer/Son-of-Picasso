using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            new Faker<ExifData>()
                .RuleFor(data => data.Id, faker => faker.Random.Int(1))
                .RuleFor(data => data.Make, faker => faker.Random.Words())
                .RuleFor(data => data.Model, faker => faker.Random.Words())
                .RuleFor(data => data.Software, faker => faker.Random.Words())
                .RuleFor(data => data.UserComment, faker => faker.Random.Words())
                .RuleFor(data => data.ImageDescription, faker => faker.Random.Words())
                .RuleFor(data => data.DocumentName, faker => faker.Random.Words())
                .RuleFor(data => data.ThumbnailImageDescription, faker => faker.Random.Words())
                .RuleFor(data => data.ThumbnailMake, faker => faker.Random.Words())
                .RuleFor(data => data.ThumbnailModel, faker => faker.Random.Words())
                .RuleFor(data => data.ThumbnailSoftware, faker => faker.Random.Words())
                .RuleFor(data => data.InteroperabilityIndex, faker => faker.Random.Words())
                .RuleFor(data => data.PixelXDimension, faker => faker.Random.UInt())
                .RuleFor(data => data.PixelYDimension, faker => faker.Random.UInt())
                .RuleFor(data => data.InteroperabilityIFDPointer, faker => faker.Random.UInt())
                .RuleFor(data => data.ThumbnailJPEGInterchangeFormat, faker => faker.Random.UInt())
                .RuleFor(data => data.ThumbnailJPEGInterchangeFormatLength, faker => faker.Random.UInt())
                .RuleFor(data => data.EXIFIFDPointer, faker => faker.Random.UInt())
                .RuleFor(data => data.ISOSpeedRatings, faker => faker.Random.UShort())
                .RuleFor(data => data.FocalLengthIn35mmFilm, faker => faker.Random.UShort())
                .RuleFor(data => data.DateTime, faker => faker.Date.Past())
                .RuleFor(data => data.DateTimeDigitized, (faker, data) => data.DateTime)
                .RuleFor(data => data.DateTimeOriginal, (faker, data) => data.DateTime)
                .RuleFor(data => data.ThumbnailDateTime, (faker, data) => data.DateTime)
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
                .RuleFor(data => data.InteroperabilityVersion, faker => faker.Random.Short().ToString())
                .StrictMode(true));

        public static Faker<Folder> FolderFaker => LazyFolderFaker.Value;
        private static readonly  Lazy<Faker<Folder>> LazyFolderFaker = new Lazy<Faker<Folder>>(() => 
            new Faker<Folder>()
                .RuleFor(folder => folder.Id, faker => faker.Random.Int(0))
                .RuleFor(folder => folder.Date, faker => faker.Date.Past().Date)
                .RuleFor(folder => folder.Images, () => new List<Image>())
                .RuleFor(folder => folder.Path, faker => faker.System.DirectoryPathWindows())
                .StrictMode(true)
                .RuleSet("withImages",
                    set => set
                        .RuleFor(folder => folder.Images, (faker, folder) =>
                        {
                            return faker.Make(4, i =>
                            {
                                var image = ImageFaker.Generate();
                                var time = faker.Date.Between(folder.Date, folder.Date.AddDays(1).AddSeconds(-1));
                                image.Folder = folder;
                                image.FolderId = folder.Id;
                                image.Path = Path.Combine(folder.Path, time.ToLongTimeString() + ".png");
                                image.ExifData.DateTime = time;
                                image.ExifData.DateTimeDigitized = time;
                                image.ExifData.DateTimeOriginal = time;
                                image.ExifData.DateTimeDigitized = time;
                                return image;
                            });
                        })
                    ));
        
        public static Faker<Album> AlbumFaker => LazyAlbumFaker.Value;
        private static readonly  Lazy<Faker<Album>> LazyAlbumFaker = new Lazy<Faker<Album>>(() => 
            new Faker<Album>()
                .RuleFor(album => album.Name, faker => faker.Random.Words(3))
                .StrictMode(true));

        public static Faker<Image> ImageFaker => LazyImageFaker.Value;
        private static readonly Lazy<Faker<Image>> LazyImageFaker = new Lazy<Faker<Image>>(() => 
            new Faker<Image>()
                .RuleFor(image => image.Id, faker => faker.Random.Int(0))
                .RuleFor(image => image.ExifData, () => ExifDataFaker)
                .RuleFor(image => image.ExifDataId, (faker, image) => image.ExifData.Id)
                .RuleFor(image => image.Folder, () => null)
                .RuleFor(image => image.FolderId, (faker, image) => 0)
                .RuleFor(image => image.AlbumImages, () => new List<AlbumImage>())
                .RuleFor(image => image.Path, (faker, image) => Path.Join(faker.System.DirectoryPathWindows(), faker.System.FileName("jpg")))
                .StrictMode(true)
                .RuleSet("withFolder", set => set.RuleFor(image => image.Id, faker => faker.Random.Int(0))
                    .RuleFor(image => image.Folder, (faker, image) =>
                    {
                        var folder = FolderFaker.Generate();
                        folder.Images.Add(image);
                        return folder;
                    })
                    .RuleFor(image => image.FolderId, (faker, image) => image.Folder.Id)
                    .RuleFor(image => image.Path, (faker, image) => Path.Join(image.Folder.Path, faker.System.FileName("jpg"))))
            );
    }
}
