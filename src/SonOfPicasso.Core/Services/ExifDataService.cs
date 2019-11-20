using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using Ardalis.GuardClauses;
using ExifLibrary;
using Serilog;
using SonOfPicasso.Core.Interfaces;
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

        public IObservable<ExifData> GetExifData(string path, bool supressWarning = false)
        {
            return Observable.Defer(() =>
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                ImageFile imageFile;
                using (var stream = _fileSystem.FileStream.Create(path, FileMode.Open, FileAccess.Read))
                {
                    imageFile = ImageFile.FromStream(stream);
                }

                var exifData = new ExifData();
                var unhandledProperties = new Dictionary<string, string>();
                foreach (var imageFileProperty in imageFile.Properties)
                {
                    var tag = imageFileProperty.Tag;
                    try
                    {
                        if (tag == ExifTag.DocumentName)
                            exifData.DocumentName = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.ImageDescription)
                            exifData.ImageDescription = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.Make)
                            exifData.Make = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.Model)
                            exifData.Model = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailImageDescription)
                            exifData.ThumbnailImageDescription = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.DateTime)
                            exifData.DateTime = ReadDateTime(imageFileProperty);
                        else if (tag == ExifTag.DateTimeDigitized)
                            exifData.DateTimeDigitized = ReadDateTime(imageFileProperty);
                        else if (tag == ExifTag.DateTimeOriginal)
                            exifData.DateTimeOriginal = ReadDateTime(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailDateTime)
                            exifData.ThumbnailDateTime = ReadDateTime(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailMake)
                            exifData.ThumbnailMake = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailModel)
                            exifData.ThumbnailModel = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailSoftware)
                            exifData.ThumbnailSoftware = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.InteroperabilityIndex)
                            exifData.InteroperabilityIndex = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.PixelXDimension)
                            exifData.PixelXDimension = ReadUintOrUShort(imageFileProperty);
                        else if (tag == ExifTag.PixelYDimension)
                            exifData.PixelYDimension = ReadUintOrUShort(imageFileProperty);
                        else if (tag == ExifTag.EXIFIFDPointer)
                            exifData.EXIFIFDPointer = ReadUint(imageFileProperty);
                        else if (tag == ExifTag.InteroperabilityIFDPointer)
                            exifData.InteroperabilityIFDPointer = ReadUint(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailJPEGInterchangeFormat)
                            exifData.ThumbnailJPEGInterchangeFormat = ReadUint(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailJPEGInterchangeFormatLength)
                            exifData.ThumbnailJPEGInterchangeFormatLength = ReadUint(imageFileProperty);
                        else if (tag == ExifTag.FNumber)
                            exifData.FNumber = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.MaxApertureValue)
                            exifData.MaxApertureValue = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.DigitalZoomRatio)
                            exifData.DigitalZoomRatio = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.XResolution)
                            exifData.XResolution = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.YResolution)
                            exifData.YResolution = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailXResolution)
                            exifData.ThumbnailXResolution = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailYResolution)
                            exifData.ThumbnailYResolution = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.ExposureTime)
                            exifData.ExposureTime = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.CompressedBitsPerPixel)
                            exifData.CompressedBitsPerPixel = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.FocalLength)
                            exifData.FocalLength = ReadURational(imageFileProperty);
                        else if (tag == ExifTag.Orientation)
                            exifData.Orientation = ReadEnumProperty<Orientation>(imageFileProperty);
                        else if (tag == ExifTag.Software)
                            exifData.Software = ReadAscii(imageFileProperty);
                        else if (tag == ExifTag.UserComment)
                            exifData.UserComment = ReadEncodedString(imageFileProperty);
                        else if (tag == ExifTag.FileSource)
                            exifData.FileSource = ReadEnumProperty<FileSource>(imageFileProperty);
                        else if (tag == ExifTag.ColorSpace)
                            exifData.ColorSpace = ReadEnumProperty<ColorSpace>(imageFileProperty);
                        else if (tag == ExifTag.ExposureMode)
                            exifData.ExposureMode = ReadEnumProperty<ExposureMode>(imageFileProperty);
                        else if (tag == ExifTag.MeteringMode)
                            exifData.MeteringMode = ReadEnumProperty<MeteringMode>(imageFileProperty);
                        else if (tag == ExifTag.LightSource)
                            exifData.LightSource = ReadEnumProperty<LightSource>(imageFileProperty);
                        else if (tag == ExifTag.SceneCaptureType)
                            exifData.SceneCaptureType = ReadEnumProperty<SceneCaptureType>(imageFileProperty);
                        else if (tag == ExifTag.ResolutionUnit)
                            exifData.ResolutionUnit = ReadEnumProperty<ResolutionUnit>(imageFileProperty);
                        else if (tag == ExifTag.YCbCrPositioning)
                            exifData.YCbCrPositioning = ReadEnumProperty<YCbCrPositioning>(imageFileProperty);
                        else if (tag == ExifTag.ExposureProgram)
                            exifData.ExposureProgram = ReadEnumProperty<ExposureProgram>(imageFileProperty);
                        else if (tag == ExifTag.Flash)
                            exifData.Flash = ReadEnumProperty<Flash>(imageFileProperty);
                        else if (tag == ExifTag.SceneType)
                            exifData.SceneType = ReadEnumProperty<SceneType>(imageFileProperty);
                        else if (tag == ExifTag.CustomRendered)
                            exifData.CustomRendered = ReadEnumProperty<CustomRendered>(imageFileProperty);
                        else if (tag == ExifTag.WhiteBalance)
                            exifData.WhiteBalance = ReadEnumProperty<WhiteBalance>(imageFileProperty);
                        else if (tag == ExifTag.Contrast)
                            exifData.Contrast = ReadEnumProperty<Contrast>(imageFileProperty);
                        else if (tag == ExifTag.Saturation)
                            exifData.Saturation = ReadEnumProperty<Saturation>(imageFileProperty);
                        else if (tag == ExifTag.Sharpness)
                            exifData.Sharpness = ReadEnumProperty<Sharpness>(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailCompression)
                            exifData.ThumbnailCompression = ReadEnumProperty<Compression>(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailOrientation)
                            exifData.ThumbnailOrientation = ReadEnumProperty<Orientation>(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailResolutionUnit)
                            exifData.ThumbnailResolutionUnit = ReadEnumProperty<ResolutionUnit>(imageFileProperty);
                        else if (tag == ExifTag.ThumbnailYCbCrPositioning)
                            exifData.ThumbnailYCbCrPositioning = ReadEnumProperty<YCbCrPositioning>(imageFileProperty);
                        else if (tag == ExifTag.ISOSpeedRatings)
                            exifData.ISOSpeedRatings = ReadUshort(imageFileProperty);
                        else if (tag == ExifTag.FocalLengthIn35mmFilm)
                            exifData.FocalLengthIn35mmFilm = ReadUshort(imageFileProperty);
                        else if (tag == ExifTag.ExifVersion)
                            exifData.ExifVersion = ReadExifVersion(imageFileProperty);
                        else if (tag == ExifTag.FlashpixVersion)
                            exifData.FlashpixVersion = ReadExifVersion(imageFileProperty);
                        else if (tag == ExifTag.InteroperabilityVersion)
                            exifData.InteroperabilityVersion = ReadExifVersion(imageFileProperty);
                        else if (tag == ExifTag.BrightnessValue)
                            exifData.BrightnessValue = ReadSRational(imageFileProperty);
                        else if (tag == ExifTag.ExposureBiasValue)
                            exifData.ExposureBiasValue = ReadSRational(imageFileProperty);
                        else if (tag == ExifTag.LensSpecification)
                            exifData.LensSpecification = ReadLensSpecification(imageFileProperty);
                        else
                            unhandledProperties.Add(tag.ToString(), imageFileProperty.GetType().Name);
                    }
                    catch (Exception e)
                    {
                        throw new SonOfPicassoException($"Error processing tag {tag.ToString()}", e);
                    }
                }

                if (!supressWarning && unhandledProperties.Any())
                    _logger.Warning("Unhandled Properties {Path} {@Properties}", path, unhandledProperties);

                return Observable.Return(exifData);
            }).SubscribeOn(_schedulerProvider.TaskPool);
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

        private static uint ReadUintOrUShort(ExifProperty imageFileProperty)
        {
            Guard.Against.Null(imageFileProperty, nameof(imageFileProperty));
            
            switch (imageFileProperty)
            {
                case ExifUShort exifUShort:
                    return exifUShort.Value;

                case ExifUInt exifUInt:
                    return exifUInt.Value;

                default:
                    throw new ArgumentOutOfRangeException();
            }
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