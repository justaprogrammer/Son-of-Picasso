using System;
using System.Drawing;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using AutoBogus;
using Bogus;
using ExifLibrary;
using Skybrud.Colors;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Tools.Extensions;
using ILogger = Serilog.ILogger;

namespace SonOfPicasso.Tools.Services
{
    public class ImageGenerationService
    {
        protected internal static Faker Faker = new Faker();

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageGenerationService(ILogger logger, IFileSystem fileSystem, ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<string> GenerateImages(int count, string fileRoot)
        {
            _logger.Debug("GenerateImages {Count} {FileRoot}", count, fileRoot);

            return Observable.Generate(
                initialState: 0,
                condition: value => value < count,
                iterate: value => value + 1,
                resultSelector: value =>
                {
                    var time = Faker.Date.Between(DateTime.Now, DateTime.Now.AddDays(-30));
                    var directory = _fileSystem.Path.Combine(fileRoot, time.ToString("yyyy-MM-dd"));
                    var directoryInfoBase = _fileSystem.Directory.CreateDirectory(directory);

                    var fileName = $"{time.ToString("s").Replace("-", "_").Replace(":", "_")}.jpg";
                    var filePath = _fileSystem.Path.Combine(directoryInfoBase.ToString(), fileName);

                    GenerateImage(filePath, 1024, 768, time);

                    return filePath;
                }, _schedulerProvider.TaskPool);
        }

        private IObservable<Unit> GenerateImage(string path, int width, int height, DateTime time)
        {
            return Observable.Start(() =>
            {
                _logger.Debug("GenerateImage {Path} {Width} {Height}", path, width, height);

                var cellHeight = height / 3;
                var cellWidth = width / 3;

                var red = Faker.Random.Int(0, 255);
                var green = Faker.Random.Int(0, 255);
                var blue = Faker.Random.Int(0, 255);

                var colors = SimilarColors(red, green, blue);

                ImageFile imageFile;
                ExifData exifData;
                
                using (var imageStream = new MemoryStream())
                {
                    using (var bitmap = new Bitmap(width, height))
                    {
                        using (var graphics = Graphics.FromImage(bitmap))
                        {
                            for (var x = 0; x < 3; x++)
                            {
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
                        }

                        bitmap.Save(imageStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    imageStream.Position = 0;

                    var autoFaker = new AutoFaker<ExifData>();
                    exifData = autoFaker.Generate();

                    imageFile = ImageFile.FromStream(imageStream);
                }

                ExifDataToImageFile(exifData, imageFile);

                using (var imageWithExifStream = new MemoryStream())
                {
                    imageFile.Save(imageWithExifStream);
                    _fileSystem.File.WriteAllBytes(path, imageWithExifStream.ToArray());
                }

                return Unit.Default;
            }, _schedulerProvider.TaskPool);
        }

        private void ExifDataToImageFile(ExifData exifData, ImageFile imageFile)
        {
            var properties = exifData.GetType().GetProperties()
                .Where(info => !info.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(exifData);
                var exifTag = (ExifTag)Enum.Parse(typeof(ExifTag), propertyInfo.Name, true);

                var exifTagType = GetExifTagType(exifTag);
                if (exifTagType == typeof(ExifAscii))
                {
                    imageFile.Properties.Set(new ExifAscii(exifTag, (string) value, Encoding.Default));
                }
                else
                {
                    _logger.Warning("Exif Tag {Tag} Type {Type} not supported", exifTag, exifTagType.Name);
                }
            }
        }

        private Type GetExifTagType(ExifTag tag)
        {
            if (tag == ExifTag.DocumentName)
                return typeof(ExifAscii);
            else if (tag == ExifTag.ImageDescription)
                return typeof(ExifAscii);
            else if (tag == ExifTag.Make)
                return typeof(ExifAscii);
            else if (tag == ExifTag.Model)
                return typeof(ExifAscii);
            else if (tag == ExifTag.ThumbnailImageDescription)
                return typeof(ExifAscii);
            else if (tag == ExifTag.DateTime)
                return typeof(ExifDateTime);
            else if (tag == ExifTag.DateTimeDigitized)
                return typeof(ExifDateTime);
            else if (tag == ExifTag.DateTimeOriginal)
                return typeof(ExifDateTime);
            else if (tag == ExifTag.ThumbnailDateTime)
                return typeof(ExifDateTime);
            else if (tag == ExifTag.ThumbnailMake)
                return typeof(ExifAscii);
            else if (tag == ExifTag.ThumbnailModel)
                return typeof(ExifAscii);
            else if (tag == ExifTag.ThumbnailSoftware)
                return typeof(ExifAscii);
            else if (tag == ExifTag.InteroperabilityIndex)
                return typeof(ExifAscii);
            else if (tag == ExifTag.PixelXDimension)
                return typeof(ExifUInt);
            else if (tag == ExifTag.EXIFIFDPointer)
                return typeof(ExifUInt);
            else if (tag == ExifTag.PixelYDimension)
                return typeof(ExifUInt);
            else if (tag == ExifTag.InteroperabilityIFDPointer)
                return typeof(ExifUInt);
            else if (tag == ExifTag.ThumbnailJPEGInterchangeFormat)
                return typeof(ExifUInt);
            else if (tag == ExifTag.ThumbnailJPEGInterchangeFormatLength)
                return typeof(ExifUInt);
            else if (tag == ExifTag.FNumber)
                return typeof(ExifURational);
            else if (tag == ExifTag.MaxApertureValue)
                return typeof(ExifURational);
            else if (tag == ExifTag.DigitalZoomRatio)
                return typeof(ExifURational);
            else if (tag == ExifTag.XResolution)
                return typeof(ExifURational);
            else if (tag == ExifTag.YResolution)
                return typeof(ExifURational);
            else if (tag == ExifTag.ThumbnailXResolution)
                return typeof(ExifURational);
            else if (tag == ExifTag.ThumbnailYResolution)
                return typeof(ExifURational);
            else if (tag == ExifTag.ExposureTime)
                return typeof(ExifURational);
            else if (tag == ExifTag.CompressedBitsPerPixel)
                return typeof(ExifURational);
            else if (tag == ExifTag.FocalLength)
                return typeof(ExifURational);
            else if (tag == ExifTag.Orientation)
                return typeof(ExifEnumProperty<Orientation>);
            else if (tag == ExifTag.Software)
                return typeof(ExifAscii);
            else if (tag == ExifTag.UserComment)
                return typeof(ExifEncodedString);
            else if (tag == ExifTag.FileSource)
                return typeof(ExifEnumProperty<FileSource>);
            else if (tag == ExifTag.ColorSpace)
                return typeof(ExifEnumProperty<ColorSpace>);
            else if (tag == ExifTag.ExposureMode)
                return typeof(ExifEnumProperty<ExposureMode>);
            else if (tag == ExifTag.MeteringMode)
                return typeof(ExifEnumProperty<MeteringMode>);
            else if (tag == ExifTag.LightSource)
                return typeof(ExifEnumProperty<LightSource>);
            else if (tag == ExifTag.SceneCaptureType)
                return typeof(ExifEnumProperty<SceneCaptureType>);
            else if (tag == ExifTag.ResolutionUnit)
                return typeof(ExifEnumProperty<ResolutionUnit>);
            else if (tag == ExifTag.YCbCrPositioning)
                return typeof(ExifEnumProperty<YCbCrPositioning>);
            else if (tag == ExifTag.ExposureProgram)
                return typeof(ExifEnumProperty<ExposureProgram>);
            else if (tag == ExifTag.Flash)
                return typeof(ExifEnumProperty<Flash>);
            else if (tag == ExifTag.SceneType)
                return typeof(ExifEnumProperty<SceneType>);
            else if (tag == ExifTag.CustomRendered)
                return typeof(ExifEnumProperty<CustomRendered>);
            else if (tag == ExifTag.WhiteBalance)
                return typeof(ExifEnumProperty<WhiteBalance>);
            else if (tag == ExifTag.Contrast)
                return typeof(ExifEnumProperty<Contrast>);
            else if (tag == ExifTag.Saturation)
                return typeof(ExifEnumProperty<Saturation>);
            else if (tag == ExifTag.Sharpness)
                return typeof(ExifEnumProperty<Sharpness>);
            else if (tag == ExifTag.ThumbnailCompression)
                return typeof(ExifEnumProperty<Compression>);
            else if (tag == ExifTag.ThumbnailOrientation)
                return typeof(ExifEnumProperty<Orientation>);
            else if (tag == ExifTag.ThumbnailResolutionUnit)
                return typeof(ExifEnumProperty<ResolutionUnit>);
            else if (tag == ExifTag.ThumbnailYCbCrPositioning)
                return typeof(ExifEnumProperty<YCbCrPositioning>);
            else if (tag == ExifTag.ISOSpeedRatings)
                return typeof(ExifUShort);
            else if (tag == ExifTag.FocalLengthIn35mmFilm)
                return typeof(ExifUShort);
            else if (tag == ExifTag.ExifVersion)
                return typeof(ExifVersion);
            else if (tag == ExifTag.FlashpixVersion)
                return typeof(ExifVersion);
            else if (tag == ExifTag.InteroperabilityVersion)
                return typeof(ExifVersion);
            else if (tag == ExifTag.BrightnessValue)
                return typeof(ExifSRational);
            else if (tag == ExifTag.ExposureBiasValue)
                return typeof(ExifSRational);
            else if (tag == ExifTag.LensSpecification)
                return typeof(LensSpecification);
            else return null;
        }

        private static Color GetColor(Color[] colors, int x, int y)
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

            return new[] { HslColorExtensions.ToColor(color0), HslColorExtensions.ToColor(color1), HslColorExtensions.ToColor(color2) };
        }
    }
}