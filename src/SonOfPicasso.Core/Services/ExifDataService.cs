using System;
using System.IO;
using System.IO.Abstractions;
using System.Reactive.Linq;
using Ardalis.GuardClauses;
using ExifLibrary;
using Serilog;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class ExifDataService : IExifDataService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ExifDataService(IFileSystem fileSystem, ILogger logger, ISchedulerProvider schedulerProvider)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _schedulerProvider = schedulerProvider;
        }

        public IObservable<ExifData> GetExifData(string path)
        {
            return Observable.Start(() =>
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                ImageFile imageFile;
                using (var stream = _fileSystem.FileStream.Create(path, FileMode.Open))
                {
                    imageFile = ImageFile.FromStream(stream);
                }

                var exifData = new ExifData();
                foreach (var imageFileProperty in imageFile.Properties)
                    if (imageFileProperty.Tag == ExifTag.DocumentName)
                        exifData.DocumentName = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ImageDescription)
                        exifData.ImageDescription = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.Make)
                        exifData.Make = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.Model)
                        exifData.Model = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailImageDescription)
                        exifData.ThumbnailImageDescription = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.DateTime)
                        exifData.DateTime = ReadDateTime(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.DateTimeDigitized)
                        exifData.DateTimeDigitized = ReadDateTime(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.DateTimeOriginal)
                        exifData.DateTimeOriginal = ReadDateTime(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailDateTime)
                        exifData.ThumbnailDateTime = ReadDateTime(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailMake)
                        exifData.ThumbnailMake = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailModel)
                        exifData.ThumbnailModel = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailSoftware)
                        exifData.ThumbnailSoftware = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.InteroperabilityIndex)
                        exifData.InteroperabilityIndex = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.PixelXDimension)
                        exifData.PixelXDimension = ReadUint(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.EXIFIFDPointer)
                        exifData.EXIFIFDPointer = ReadUint(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.PixelYDimension)
                        exifData.PixelYDimension = ReadUint(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.InteroperabilityIFDPointer)
                        exifData.InteroperabilityIFDPointer = ReadUint(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailJPEGInterchangeFormat)
                        exifData.ThumbnailJPEGInterchangeFormat = ReadUint(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailJPEGInterchangeFormatLength)
                        exifData.ThumbnailJPEGInterchangeFormatLength = ReadUint(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.FNumber)
                        exifData.FNumber = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.MaxApertureValue)
                        exifData.MaxApertureValue = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.DigitalZoomRatio)
                        exifData.DigitalZoomRatio = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.XResolution)
                        exifData.XResolution = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.YResolution)
                        exifData.YResolution = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailXResolution)
                        exifData.ThumbnailXResolution = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailYResolution)
                        exifData.ThumbnailYResolution = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ExposureTime)
                        exifData.ExposureTime = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.CompressedBitsPerPixel)
                        exifData.CompressedBitsPerPixel = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.FocalLength)
                        exifData.FocalLength = ReadURational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.Orientation)
                        exifData.Orientation = ReadEnumProperty<Orientation>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.Software)
                        exifData.Software = ReadAscii(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.UserComment)
                        exifData.UserComment = ReadEncodedString(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.FileSource)
                        exifData.FileSource = ReadEnumProperty<FileSource>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ColorSpace)
                        exifData.ColorSpace = ReadEnumProperty<ColorSpace>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ExposureMode)
                        exifData.ExposureMode = ReadEnumProperty<ExposureMode>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.MeteringMode)
                        exifData.MeteringMode = ReadEnumProperty<MeteringMode>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.LightSource)
                        exifData.LightSource = ReadEnumProperty<LightSource>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.SceneCaptureType)
                        exifData.SceneCaptureType = ReadEnumProperty<SceneCaptureType>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ResolutionUnit)
                        exifData.ResolutionUnit = ReadEnumProperty<ResolutionUnit>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.YCbCrPositioning)
                        exifData.YCbCrPositioning = ReadEnumProperty<YCbCrPositioning>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ExposureProgram)
                        exifData.ExposureProgram = ReadEnumProperty<ExposureProgram>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.Flash)
                        exifData.Flash = ReadEnumProperty<Flash>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.SceneType)
                        exifData.SceneType = ReadEnumProperty<SceneType>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.CustomRendered)
                        exifData.CustomRendered = ReadEnumProperty<CustomRendered>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.WhiteBalance)
                        exifData.WhiteBalance = ReadEnumProperty<WhiteBalance>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.Contrast)
                        exifData.Contrast = ReadEnumProperty<Contrast>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.Saturation)
                        exifData.Saturation = ReadEnumProperty<Saturation>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.Sharpness)
                        exifData.Sharpness = ReadEnumProperty<Sharpness>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailCompression)
                        exifData.ThumbnailCompression = ReadEnumProperty<Compression>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailOrientation)
                        exifData.ThumbnailOrientation = ReadEnumProperty<Orientation>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailResolutionUnit)
                        exifData.ThumbnailResolutionUnit = ReadEnumProperty<ResolutionUnit>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ThumbnailYCbCrPositioning)
                        exifData.ThumbnailYCbCrPositioning = ReadEnumProperty<YCbCrPositioning>(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ISOSpeedRatings)
                        exifData.ISOSpeedRatings = ReadUshort(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.FocalLengthIn35mmFilm)
                        exifData.FocalLengthIn35mmFilm = ReadUshort(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ExifVersion)
                        exifData.ExifVersion = ReadExifVersion(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.FlashpixVersion)
                        exifData.FlashpixVersion = ReadExifVersion(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.InteroperabilityVersion)
                        exifData.InteroperabilityVersion = ReadExifVersion(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.BrightnessValue)
                        exifData.BrightnessValue = ReadSRational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.ExposureBiasValue)
                        exifData.ExposureBiasValue = ReadSRational(imageFileProperty);
                    else if (imageFileProperty.Tag == ExifTag.LensSpecification)
                        exifData.LensSpecification = ReadLensSpecification(imageFileProperty);
                    else
                        _logger.Verbose("Unhandled Exif Property {Tag} {Type}", imageFileProperty.Tag,
                            imageFileProperty.GetType().Name);

                return exifData;
            }, _schedulerProvider.TaskPool);
        }

        private string ReadLensSpecification(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var lensSpecification = (LensSpecification) imageFileProperty;
            return lensSpecification.ToString();
        }

        private string ReadExifVersion(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var exifVersion = (ExifVersion) imageFileProperty;
            return exifVersion.Value;
        }

        private string ReadEncodedString(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var exifEncodedString = (ExifEncodedString) imageFileProperty;
            return exifEncodedString.Value;
        }

        private static string ReadURational(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var exifURational = (ExifURational) imageFileProperty;
            return exifURational.Value.ToString();
        }

        private static string ReadSRational(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var exifSRational = (ExifSRational) imageFileProperty;
            return exifSRational.Value.ToString();
        }

        private static uint ReadUint(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var exifUInt = (ExifUInt) imageFileProperty;
            return exifUInt.Value;
        }

        private static ushort ReadUshort(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var exifUShort = (ExifUShort) imageFileProperty;
            return exifUShort.Value;
        }

        private static DateTime ReadDateTime(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var exifDateTime = (ExifDateTime) imageFileProperty;
            return exifDateTime.Value;
        }

        private static string ReadAscii(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            var exifAscii = (ExifAscii) imageFileProperty;
            return exifAscii.Value;
        }

        private static string ReadEnumProperty<T>(ExifProperty exifProperty)
        {
            Guard.Against.Null(exifProperty, nameof(exifProperty));
            var exifEnumProperty = (ExifEnumProperty<T>) exifProperty;
            return exifEnumProperty.Value.ToString();
        }
    }
}