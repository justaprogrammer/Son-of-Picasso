using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bogus;
using ExifLibrary;
using Serilog;
using Skybrud.Colors;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Tools.Extensions;

namespace SonOfPicasso.Tools.Services
{
    public class ImageGenerationService
    {
        protected internal static Faker Faker = new Faker();

        private static readonly (int, int)[] Sizes =
        {
            (900, 1500),
            (900, 1500),
            (1200, 1800),
            (1500, 2100),
            (2400, 2400),
            (2400, 3000),
            (2550, 3300),
            (2700, 4800),
            (3300, 4200),
            (3300, 4800),
            (1500, 900),
            (1500, 900),
            (1800, 1200),
            (2100, 1500),
            (3000, 2400),
            (3300, 2550),
            (4800, 2700),
            (4200, 3300),
            (4800, 3300)
        };

        private readonly IFileSystem _fileSystem;

        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageGenerationService(ILogger logger, IFileSystem fileSystem, ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<IGroupedObservable<string, string>> GenerateImages(int count, string fileRoot,
            bool inDateNamedDirectory = true)
        {
            return Observable.Generate(
                    0,
                    value => value < count,
                    value => value + 1,
                    value =>
                    {
                        if (value == 0)
                            _logger.Verbose("GenerateImages {Count} {FileRoot}", count, fileRoot);

                        var time = Faker.Date.Between(DateTime.Now, DateTime.Now.AddDays(-30));
                        var directoryPath = inDateNamedDirectory
                            ? _fileSystem.Path.Combine(fileRoot, time.ToString("yyyy-MM-dd"))
                            : fileRoot;
                        _fileSystem.Directory.CreateDirectory(directoryPath);

                        var fileName = $"{time.ToString("s").Replace("-", "_").Replace(":", "_")}.jpg";
                        var filePath = _fileSystem.Path.Combine(directoryPath, fileName);

                        var exifData = (ExifData) Fakers.ExifDataFaker;
                        exifData.DateTime = time;
                        exifData.DateTimeDigitized = time;
                        exifData.DateTimeOriginal = time;

                        return GenerateImage(filePath, exifData)
                            .Select(imagePath => (directoryPath, imagePath));
                    }, _schedulerProvider.TaskPool)
                .SelectMany(observable => observable)
                .GroupBy(tuple => tuple.directoryPath, tuple => tuple.imagePath);
        }

        public IObservable<string> GenerateImage(string path, ExifData exifData)
        {
            return Observable.Defer(() =>
            {
                var (height, width) = Faker.PickRandom(Sizes);

                ImageFile imageFile;

                using (var imageStream = new MemoryStream())
                {
                    using (var bitmap = new Bitmap(width, height))
                    {
                        var cellSize = Faker.Random.Int(3, 10);

                        using var graphics = Graphics.FromImage(bitmap);

                        foreach (var y in ChunkValues(height, cellSize))
                        foreach (var x in ChunkValues(width, cellSize))
                        {
                            using var brush = new SolidBrush(Color.FromKnownColor(Faker.PickRandom<KnownColor>()));
                            var rectangle = new Rectangle(x, y, cellSize, cellSize);
                            graphics.FillRectangle(brush, rectangle);
                        }

                        bitmap.Save(imageStream, ImageFormat.Jpeg);
                    }

                    imageStream.Position = 0;

                    imageFile = ImageFile.FromStream(imageStream);
                }

                CopyExifDataToImageFile(exifData, imageFile);

                using (var imageWithExifStream = new MemoryStream())
                {
                    imageFile.Save(imageWithExifStream);
                    _fileSystem.File.WriteAllBytes(path, imageWithExifStream.ToArray());
                }

                return Observable.Return(path);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        private static IEnumerable<int> ChunkValues(int max, int chunk)
        {
            for (var x = 0; x < max / chunk + 1; x++) yield return x * chunk;

            if (max % chunk != 0) yield return max;
        }

        internal void CopyExifDataToImageFile(ExifData exifData, ImageFile imageFile)
        {
            var properties = exifData.GetType().GetProperties()
                .Where(info => !info.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            foreach (var propertyInfo in properties)
                try
                {
                    var exifTag = (ExifTag) Enum.Parse(typeof(ExifTag), propertyInfo.Name, true);

                    var exifTagType = GetExifTagType(exifTag);
                    if (exifTagType == typeof(ExifAscii))
                    {
                        var value = (string) propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifAscii(exifTag, value, Encoding.Default);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifEncodedString))
                    {
                        var value = (string) propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifEncodedString(exifTag, value, Encoding.Default);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifUShort))
                    {
                        var value = (ushort) propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifUShort(exifTag, value);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifUInt))
                    {
                        var value = (uint) propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifUInt(exifTag, value);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifDateTime))
                    {
                        var value = (DateTime) propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifDateTime(exifTag, value);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifVersion))
                    {
                        var value = (string) propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifVersion(exifTag, value);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifURational))
                    {
                        var value = (string) propertyInfo.GetValue(exifData);
                        var uFraction = MathEx.UFraction32.Parse(value);
                        var exifProperty = new ExifURational(exifTag, uFraction);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifSRational))
                    {
                        var value = (string) propertyInfo.GetValue(exifData);
                        var fraction = MathEx.Fraction32.Parse(value);
                        var exifProperty = new ExifSRational(exifTag, fraction);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(LensSpecification))
                    {
                        var value = (string) propertyInfo.GetValue(exifData);
                        var regex = new Regex("^(.*?) F(.*?), (.*?) F(.*?)$");
                        var match = regex.Match(value);
                        var fractions = match.Groups.Values.Skip(1)
                            .Select(group => MathEx.UFraction32.Parse(group.Value))
                            .ToArray();
                        var exifProperty = new LensSpecification(exifTag,
                            new[] {fractions[0], fractions[2], fractions[1], fractions[3]});
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType.IsGenericType &&
                             exifTagType.GetGenericTypeDefinition() == typeof(ExifEnumProperty<>))
                    {
                        var enumType = exifTagType.GenericTypeArguments.First();
                        var value = (string) propertyInfo.GetValue(exifData);
                        var enumValue = Enum.Parse(enumType, value, true);
                        var constructorInfo = typeof(ExifEnumProperty<>).MakeGenericType(enumType)
                            .GetConstructor(new[] {typeof(ExifTag), enumType});

                        if (constructorInfo == null)
                            throw new InvalidOperationException(
                                $"Constructor not found for enum type: '{enumType.Name}'");

                        var exitProperty = (ExifProperty) constructorInfo.Invoke(new[] {exifTag, enumValue});
                        imageFile.Properties.Add(exitProperty);
                    }
                    else
                    {
                        _logger.Warning("Exif Tag {Tag} Type {Type} not supported", exifTag, exifTagType.Name);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Error Processing Exif Data Field '{propertyInfo.Name}'", e);
                }
        }

        private Type GetExifTagType(ExifTag tag)
        {
            if (tag == ExifTag.DocumentName)
                return typeof(ExifAscii);
            if (tag == ExifTag.ImageDescription)
                return typeof(ExifAscii);
            if (tag == ExifTag.Make)
                return typeof(ExifAscii);
            if (tag == ExifTag.Model)
                return typeof(ExifAscii);
            if (tag == ExifTag.ThumbnailImageDescription)
                return typeof(ExifAscii);
            if (tag == ExifTag.DateTime)
                return typeof(ExifDateTime);
            if (tag == ExifTag.DateTimeDigitized)
                return typeof(ExifDateTime);
            if (tag == ExifTag.DateTimeOriginal)
                return typeof(ExifDateTime);
            if (tag == ExifTag.ThumbnailDateTime)
                return typeof(ExifDateTime);
            if (tag == ExifTag.ThumbnailMake)
                return typeof(ExifAscii);
            if (tag == ExifTag.ThumbnailModel)
                return typeof(ExifAscii);
            if (tag == ExifTag.ThumbnailSoftware)
                return typeof(ExifAscii);
            if (tag == ExifTag.InteroperabilityIndex)
                return typeof(ExifAscii);
            if (tag == ExifTag.PixelXDimension)
                return typeof(ExifUInt);
            if (tag == ExifTag.EXIFIFDPointer)
                return typeof(ExifUInt);
            if (tag == ExifTag.PixelYDimension)
                return typeof(ExifUInt);
            if (tag == ExifTag.InteroperabilityIFDPointer)
                return typeof(ExifUInt);
            if (tag == ExifTag.ThumbnailJPEGInterchangeFormat)
                return typeof(ExifUInt);
            if (tag == ExifTag.ThumbnailJPEGInterchangeFormatLength)
                return typeof(ExifUInt);
            if (tag == ExifTag.FNumber)
                return typeof(ExifURational);
            if (tag == ExifTag.MaxApertureValue)
                return typeof(ExifURational);
            if (tag == ExifTag.DigitalZoomRatio)
                return typeof(ExifURational);
            if (tag == ExifTag.XResolution)
                return typeof(ExifURational);
            if (tag == ExifTag.YResolution)
                return typeof(ExifURational);
            if (tag == ExifTag.ThumbnailXResolution)
                return typeof(ExifURational);
            if (tag == ExifTag.ThumbnailYResolution)
                return typeof(ExifURational);
            if (tag == ExifTag.ExposureTime)
                return typeof(ExifURational);
            if (tag == ExifTag.CompressedBitsPerPixel)
                return typeof(ExifURational);
            if (tag == ExifTag.FocalLength)
                return typeof(ExifURational);
            if (tag == ExifTag.Orientation)
                return typeof(ExifEnumProperty<Orientation>);
            if (tag == ExifTag.Software)
                return typeof(ExifAscii);
            if (tag == ExifTag.UserComment)
                return typeof(ExifEncodedString);
            if (tag == ExifTag.FileSource)
                return typeof(ExifEnumProperty<FileSource>);
            if (tag == ExifTag.ColorSpace)
                return typeof(ExifEnumProperty<ColorSpace>);
            if (tag == ExifTag.ExposureMode)
                return typeof(ExifEnumProperty<ExposureMode>);
            if (tag == ExifTag.MeteringMode)
                return typeof(ExifEnumProperty<MeteringMode>);
            if (tag == ExifTag.LightSource)
                return typeof(ExifEnumProperty<LightSource>);
            if (tag == ExifTag.SceneCaptureType)
                return typeof(ExifEnumProperty<SceneCaptureType>);
            if (tag == ExifTag.ResolutionUnit)
                return typeof(ExifEnumProperty<ResolutionUnit>);
            if (tag == ExifTag.YCbCrPositioning)
                return typeof(ExifEnumProperty<YCbCrPositioning>);
            if (tag == ExifTag.ExposureProgram)
                return typeof(ExifEnumProperty<ExposureProgram>);
            if (tag == ExifTag.Flash)
                return typeof(ExifEnumProperty<Flash>);
            if (tag == ExifTag.SceneType)
                return typeof(ExifEnumProperty<SceneType>);
            if (tag == ExifTag.CustomRendered)
                return typeof(ExifEnumProperty<CustomRendered>);
            if (tag == ExifTag.WhiteBalance)
                return typeof(ExifEnumProperty<WhiteBalance>);
            if (tag == ExifTag.Contrast)
                return typeof(ExifEnumProperty<Contrast>);
            if (tag == ExifTag.Saturation)
                return typeof(ExifEnumProperty<Saturation>);
            if (tag == ExifTag.Sharpness)
                return typeof(ExifEnumProperty<Sharpness>);
            if (tag == ExifTag.ThumbnailCompression)
                return typeof(ExifEnumProperty<Compression>);
            if (tag == ExifTag.ThumbnailOrientation)
                return typeof(ExifEnumProperty<Orientation>);
            if (tag == ExifTag.ThumbnailResolutionUnit)
                return typeof(ExifEnumProperty<ResolutionUnit>);
            if (tag == ExifTag.ThumbnailYCbCrPositioning)
                return typeof(ExifEnumProperty<YCbCrPositioning>);
            if (tag == ExifTag.ISOSpeedRatings)
                return typeof(ExifUShort);
            if (tag == ExifTag.FocalLengthIn35mmFilm)
                return typeof(ExifUShort);
            if (tag == ExifTag.ExifVersion)
                return typeof(ExifVersion);
            if (tag == ExifTag.FlashpixVersion)
                return typeof(ExifVersion);
            if (tag == ExifTag.InteroperabilityVersion)
                return typeof(ExifVersion);
            if (tag == ExifTag.BrightnessValue)
                return typeof(ExifSRational);
            if (tag == ExifTag.ExposureBiasValue)
                return typeof(ExifSRational);
            if (tag == ExifTag.LensSpecification)
                return typeof(LensSpecification);
            return null;
        }

        private static Color GetColor(IReadOnlyList<Color> colors, int x, int y)
        {
            switch (x)
            {
                case 0:
                    switch (y)
                    {
                        case 0:
                            return colors[0];
                        case 1:
                            return colors[1];
                        case 2:
                            return colors[0];
                    }

                    break;
                case 1:
                    switch (y)
                    {
                        case 0:
                            return colors[1];
                        case 1:
                            return colors[2];
                        case 2:
                            return colors[1];
                    }

                    break;
                case 2:
                    switch (y)
                    {
                        case 0:
                            return colors[0];
                        case 1:
                            return colors[1];
                        case 2:
                            return colors[0];
                    }

                    break;
            }

            throw new NotSupportedException();
        }

        private static Color[] SimilarColors(int red, int green, int blue)
        {
            var color0 = new RgbColor(red, green, blue).ToHsl();

            var delta = Faker.Random.Int(25, 30) / 360.0;
            var color1 = new HslColor((color0.H + delta) % 1.0, color0.S, color0.L);
            var color2 = new HslColor((color0.H - delta) % 1.0, color0.S, color0.L);

            return new[]
            {
                color0.ToColor(),
                color1.ToColor(),
                color2.ToColor()
            };
        }
    }
}