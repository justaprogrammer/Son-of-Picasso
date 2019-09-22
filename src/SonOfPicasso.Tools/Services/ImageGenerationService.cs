﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AutoBogus;
using Bogus;
using ExifLibrary;
using Serilog;
using Skybrud.Colors;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Tools.Extensions;

namespace SonOfPicasso.Tools.Services
{
    public class ImageGenerationService
    {
        protected internal static Faker Faker = new Faker();
        internal static readonly Faker<ExifData> ExifDataFaker;
        private readonly IFileSystem _fileSystem;

        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        static ImageGenerationService()
        {
            ExifDataFaker = new AutoFaker<ExifData>()
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
                .RuleFor(data => data.InteroperabilityVersion, faker => faker.Random.Short().ToString());
        }

        public ImageGenerationService(ILogger logger, IFileSystem fileSystem, ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<IGroupedObservable<string, string>> GenerateImages(int count, string fileRoot)
        {
            return Observable.Generate(
                initialState: 0,
                condition: value => value < count,
                iterate: value => value + 1,
                resultSelector: value =>
                {
                    _logger.Debug("GenerateImages {Count} {FileRoot}", count, fileRoot);

                    var time = Faker.Date.Between(DateTime.Now, DateTime.Now.AddDays(-30));
                    var directoryPath = _fileSystem.Path.Combine(fileRoot, time.ToString("yyyy-MM-dd"));
                    _fileSystem.Directory.CreateDirectory(directoryPath);

                    var fileName = $"{time.ToString("s").Replace("-", "_").Replace(":", "_")}.jpg";
                    var filePath = _fileSystem.Path.Combine(directoryPath, fileName);

                    var exifData = (ExifData)ExifDataFaker;
                    exifData.DateTime = time;
                    exifData.DateTimeDigitized = time;
                    exifData.DateTimeOriginal = time;

                    return GenerateImage(filePath, 1024, 768, exifData)
                        .Select(imagePath => (directoryPath, imagePath));

                }, _schedulerProvider.TaskPool)
                .SelectMany(observable => observable)
                .GroupBy(tuple => tuple.directoryPath, tuple => tuple.imagePath);
        }

        public IObservable<string> GenerateImage(string path, int width, int height, ExifData exifData)
        {
            return Observable.Start(() =>
            {
                var cellHeight = height / 3;
                var cellWidth = width / 3;

                var red = Faker.Random.Int(0, 255);
                var green = Faker.Random.Int(0, 255);
                var blue = Faker.Random.Int(0, 255);

                var colors = SimilarColors(red, green, blue);

                ImageFile imageFile;

                using (var imageStream = new MemoryStream())
                {
                    using (var bitmap = new Bitmap(width, height))
                    {
                        using (var graphics = Graphics.FromImage(bitmap))
                        {
                            for (var x = 0; x < 3; x++)
                                for (var y = 0; y < 3; y++)
                                {
                                    var xPos = x * cellWidth;
                                    var yPos = y * cellHeight;

                                    using (var brush = new SolidBrush(GetColor(colors, x, y)))
                                    {
                                        var rectangle = new Rectangle(xPos, yPos, cellWidth, cellHeight);
                                        graphics.FillRectangle(brush, rectangle);
                                    }
                                }
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

                return path;
            }, _schedulerProvider.TaskPool);
        }

        internal void CopyExifDataToImageFile(ExifData exifData, ImageFile imageFile)
        {
            var properties = exifData.GetType().GetProperties()
                .Where(info => !info.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            foreach (var propertyInfo in properties)
                try
                {
                    var exifTag = (ExifTag)Enum.Parse(typeof(ExifTag), propertyInfo.Name, true);

                    var exifTagType = GetExifTagType(exifTag);
                    if (exifTagType == typeof(ExifAscii))
                    {
                        var value = (string)propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifAscii(exifTag, value, Encoding.Default);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifEncodedString))
                    {
                        var value = (string)propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifEncodedString(exifTag, value, Encoding.Default);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifUShort))
                    {
                        var value = (ushort)propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifUShort(exifTag, value);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifUInt))
                    {
                        var value = (uint)propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifUInt(exifTag, value);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifDateTime))
                    {
                        var value = (DateTime)propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifDateTime(exifTag, value);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifVersion))
                    {
                        var value = (string)propertyInfo.GetValue(exifData);
                        var exifProperty = new ExifVersion(exifTag, value);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifURational))
                    {
                        var value = (string)propertyInfo.GetValue(exifData);
                        var uFraction = MathEx.UFraction32.Parse(value);
                        var exifProperty = new ExifURational(exifTag, uFraction);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(ExifSRational))
                    {
                        var value = (string)propertyInfo.GetValue(exifData);
                        var fraction = MathEx.Fraction32.Parse(value);
                        var exifProperty = new ExifSRational(exifTag, fraction);
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType == typeof(LensSpecification))
                    {
                        var value = (string)propertyInfo.GetValue(exifData);
                        var regex = new Regex("^(.*?) F(.*?), (.*?) F(.*?)$");
                        var match = regex.Match(value);
                        var fractions = match.Groups.Values.Skip(1)
                            .Select(group => MathEx.UFraction32.Parse(group.Value))
                            .ToArray();
                        var exifProperty = new LensSpecification(exifTag,
                            new[] { fractions[0], fractions[2], fractions[1], fractions[3] });
                        imageFile.Properties.Set(exifProperty);
                    }
                    else if (exifTagType.IsGenericType &&
                             exifTagType.GetGenericTypeDefinition() == typeof(ExifEnumProperty<>))
                    {
                        var enumType = exifTagType.GenericTypeArguments.First();
                        var value = (string)propertyInfo.GetValue(exifData);
                        var enumValue = Enum.Parse(enumType, value, true);
                        var constructorInfo = typeof(ExifEnumProperty<>).MakeGenericType(enumType)
                            .GetConstructor(new[] { typeof(ExifTag), enumType });

                        if (constructorInfo == null)
                            throw new InvalidOperationException(
                                $"Constructor not found for enum type: '{enumType.Name}'");

                        var exitProperty = (ExifProperty)constructorInfo.Invoke(new[] { exifTag, enumValue });
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
                HslColorExtensions.ToColor(color0), HslColorExtensions.ToColor(color1),
                HslColorExtensions.ToColor(color2)
            };
        }
    }
}