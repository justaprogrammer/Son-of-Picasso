using System;
using System.IO;
using System.IO.Abstractions;
using ExifLibrary;

namespace SonOfPicasso.Core.Services
{
    public class ExifData
    {
        public string Make { get; set; }
        public string Model { get; set; }
        public string Software { get; set; }
        public string UserComment { get; set; }
        public string FileSource { get; set; }
    }

    public class ExifDataService: IExifDataService
    {
        private readonly IFileSystem _fileSystem;

        public ExifDataService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public ExifData GetExifData(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            ImageFile imageFile;
            using (var stream = _fileSystem.FileStream.Create(path, FileMode.Open))
            {
                imageFile = ImageFile.FromStream(stream);
            }

            var exifData = new ExifData();
            foreach (var imageFileProperty in imageFile.Properties)
            {
                var exifEncodedString = imageFileProperty as ExifEncodedString;
                var exifAscii = imageFileProperty as ExifAscii;

                switch (imageFileProperty.Tag)
                {
                    case ExifTag.NewSubfileType:
                        break;
                    case ExifTag.SubfileType:
                        break;
                    case ExifTag.ImageWidth:
                        break;
                    case ExifTag.ImageLength:
                        break;
                    case ExifTag.BitsPerSample:
                        break;
                    case ExifTag.Compression:
                        break;
                    case ExifTag.PhotometricInterpretation:
                        break;
                    case ExifTag.Threshholding:
                        break;
                    case ExifTag.CellWidth:
                        break;
                    case ExifTag.CellLength:
                        break;
                    case ExifTag.FillOrder:
                        break;
                    case ExifTag.DocumentName:
                        break;
                    case ExifTag.ImageDescription:
                        break;
                    case ExifTag.Make:
                        exifData.Make = exifAscii?.Value;
                        break;
                    case ExifTag.Model:
                        exifData.Model = exifAscii?.Value;
                        break;
                    case ExifTag.StripOffsets:
                        break;
                    case ExifTag.Orientation:
                        break;
                    case ExifTag.SamplesPerPixel:
                        break;
                    case ExifTag.RowsPerStrip:
                        break;
                    case ExifTag.StripByteCounts:
                        break;
                    case ExifTag.MinSampleValue:
                        break;
                    case ExifTag.MaxSampleValue:
                        break;
                    case ExifTag.XResolution:
                        break;
                    case ExifTag.YResolution:
                        break;
                    case ExifTag.PlanarConfiguration:
                        break;
                    case ExifTag.PageName:
                        break;
                    case ExifTag.XPosition:
                        break;
                    case ExifTag.YPosition:
                        break;
                    case ExifTag.FreeOffsets:
                        break;
                    case ExifTag.FreeByteCounts:
                        break;
                    case ExifTag.GrayResponseUnit:
                        break;
                    case ExifTag.GrayResponseCurve:
                        break;
                    case ExifTag.T4Options:
                        break;
                    case ExifTag.T6Options:
                        break;
                    case ExifTag.ResolutionUnit:
                        break;
                    case ExifTag.PageNumber:
                        break;
                    case ExifTag.TransferFunction:
                        break;
                    case ExifTag.Software:
                        exifData.Software = exifAscii?.Value;
                        break;
                    case ExifTag.DateTime:
                        break;
                    case ExifTag.Artist:
                        break;
                    case ExifTag.HostComputer:
                        break;
                    case ExifTag.Predictor:
                        break;
                    case ExifTag.WhitePoint:
                        break;
                    case ExifTag.PrimaryChromaticities:
                        break;
                    case ExifTag.ColorMap:
                        break;
                    case ExifTag.HalftoneHints:
                        break;
                    case ExifTag.TileWidth:
                        break;
                    case ExifTag.TileLength:
                        break;
                    case ExifTag.TileOffsets:
                        break;
                    case ExifTag.TileByteCounts:
                        break;
                    case ExifTag.InkSet:
                        break;
                    case ExifTag.InkNames:
                        break;
                    case ExifTag.NumberOfInks:
                        break;
                    case ExifTag.DotRange:
                        break;
                    case ExifTag.TargetPrinter:
                        break;
                    case ExifTag.ExtraSamples:
                        break;
                    case ExifTag.SampleFormat:
                        break;
                    case ExifTag.SMinSampleValue:
                        break;
                    case ExifTag.SMaxSampleValue:
                        break;
                    case ExifTag.TransferRange:
                        break;
                    case ExifTag.JPEGProc:
                        break;
                    case ExifTag.JPEGInterchangeFormat:
                        break;
                    case ExifTag.JPEGInterchangeFormatLength:
                        break;
                    case ExifTag.JPEGRestartInterval:
                        break;
                    case ExifTag.JPEGLosslessPredictors:
                        break;
                    case ExifTag.JPEGPointTransforms:
                        break;
                    case ExifTag.JPEGQTables:
                        break;
                    case ExifTag.JPEGDCTables:
                        break;
                    case ExifTag.JPEGACTables:
                        break;
                    case ExifTag.YCbCrCoefficients:
                        break;
                    case ExifTag.YCbCrSubSampling:
                        break;
                    case ExifTag.YCbCrPositioning:
                        break;
                    case ExifTag.ReferenceBlackWhite:
                        break;
                    case ExifTag.Copyright:
                        break;
                    case ExifTag.EXIFIFDPointer:
                        break;
                    case ExifTag.GPSIFDPointer:
                        break;
                    case ExifTag.WindowsTitle:
                        break;
                    case ExifTag.WindowsComment:
                        break;
                    case ExifTag.WindowsAuthor:
                        break;
                    case ExifTag.WindowsKeywords:
                        break;
                    case ExifTag.WindowsSubject:
                        break;
                    case ExifTag.Rating:
                        break;
                    case ExifTag.RatingPercent:
                        break;
                    case ExifTag.ZerothIFDPadding:
                        break;
                    case ExifTag.ExifVersion:
                        break;
                    case ExifTag.FlashpixVersion:
                        break;
                    case ExifTag.ColorSpace:
                        break;
                    case ExifTag.ComponentsConfiguration:
                        break;
                    case ExifTag.CompressedBitsPerPixel:
                        break;
                    case ExifTag.PixelXDimension:
                        break;
                    case ExifTag.PixelYDimension:
                        break;
                    case ExifTag.MakerNote:
                        break;
                    case ExifTag.UserComment:
                        exifData.UserComment = exifEncodedString?.Value;
                        break;
                    case ExifTag.RelatedSoundFile:
                        break;
                    case ExifTag.DateTimeOriginal:
                        break;
                    case ExifTag.DateTimeDigitized:
                        break;
                    case ExifTag.SubSecTime:
                        break;
                    case ExifTag.SubSecTimeOriginal:
                        break;
                    case ExifTag.SubSecTimeDigitized:
                        break;
                    case ExifTag.ExposureTime:
                        break;
                    case ExifTag.FNumber:
                        break;
                    case ExifTag.ExposureProgram:
                        break;
                    case ExifTag.SpectralSensitivity:
                        break;
                    case ExifTag.ISOSpeedRatings:
                        break;
                    case ExifTag.OECF:
                        break;
                    case ExifTag.ShutterSpeedValue:
                        break;
                    case ExifTag.ApertureValue:
                        break;
                    case ExifTag.BrightnessValue:
                        break;
                    case ExifTag.ExposureBiasValue:
                        break;
                    case ExifTag.MaxApertureValue:
                        break;
                    case ExifTag.SubjectDistance:
                        break;
                    case ExifTag.MeteringMode:
                        break;
                    case ExifTag.LightSource:
                        break;
                    case ExifTag.Flash:
                        break;
                    case ExifTag.FocalLength:
                        break;
                    case ExifTag.SubjectArea:
                        break;
                    case ExifTag.FlashEnergy:
                        break;
                    case ExifTag.SpatialFrequencyResponse:
                        break;
                    case ExifTag.FocalPlaneXResolution:
                        break;
                    case ExifTag.FocalPlaneYResolution:
                        break;
                    case ExifTag.FocalPlaneResolutionUnit:
                        break;
                    case ExifTag.SubjectLocation:
                        break;
                    case ExifTag.ExposureIndex:
                        break;
                    case ExifTag.SensingMethod:
                        break;
                    case ExifTag.FileSource:
                        exifData.FileSource = ((ExifEnumProperty<FileSource>) imageFileProperty).Value.ToString();
                        break;
                    case ExifTag.SceneType:
                        break;
                    case ExifTag.CFAPattern:
                        break;
                    case ExifTag.CustomRendered:
                        break;
                    case ExifTag.ExposureMode:
                        break;
                    case ExifTag.WhiteBalance:
                        break;
                    case ExifTag.DigitalZoomRatio:
                        break;
                    case ExifTag.FocalLengthIn35mmFilm:
                        break;
                    case ExifTag.SceneCaptureType:
                        break;
                    case ExifTag.GainControl:
                        break;
                    case ExifTag.Contrast:
                        break;
                    case ExifTag.Saturation:
                        break;
                    case ExifTag.Sharpness:
                        break;
                    case ExifTag.DeviceSettingDescription:
                        break;
                    case ExifTag.SubjectDistanceRange:
                        break;
                    case ExifTag.ImageUniqueID:
                        break;
                    case ExifTag.CameraOwnerName:
                        break;
                    case ExifTag.BodySerialNumber:
                        break;
                    case ExifTag.LensSpecification:
                        break;
                    case ExifTag.LensMake:
                        break;
                    case ExifTag.LensModel:
                        break;
                    case ExifTag.LensSerialNumber:
                        break;
                    case ExifTag.InteroperabilityIFDPointer:
                        break;
                    case ExifTag.ExifIFDPadding:
                        break;
                    case ExifTag.OffsetSchema:
                        break;
                    case ExifTag.GPSVersionID:
                        break;
                    case ExifTag.GPSLatitudeRef:
                        break;
                    case ExifTag.GPSLatitude:
                        break;
                    case ExifTag.GPSLongitudeRef:
                        break;
                    case ExifTag.GPSLongitude:
                        break;
                    case ExifTag.GPSAltitudeRef:
                        break;
                    case ExifTag.GPSAltitude:
                        break;
                    case ExifTag.GPSTimeStamp:
                        break;
                    case ExifTag.GPSSatellites:
                        break;
                    case ExifTag.GPSStatus:
                        break;
                    case ExifTag.GPSMeasureMode:
                        break;
                    case ExifTag.GPSDOP:
                        break;
                    case ExifTag.GPSSpeedRef:
                        break;
                    case ExifTag.GPSSpeed:
                        break;
                    case ExifTag.GPSTrackRef:
                        break;
                    case ExifTag.GPSTrack:
                        break;
                    case ExifTag.GPSImgDirectionRef:
                        break;
                    case ExifTag.GPSImgDirection:
                        break;
                    case ExifTag.GPSMapDatum:
                        break;
                    case ExifTag.GPSDestLatitudeRef:
                        break;
                    case ExifTag.GPSDestLatitude:
                        break;
                    case ExifTag.GPSDestLongitudeRef:
                        break;
                    case ExifTag.GPSDestLongitude:
                        break;
                    case ExifTag.GPSDestBearingRef:
                        break;
                    case ExifTag.GPSDestBearing:
                        break;
                    case ExifTag.GPSDestDistanceRef:
                        break;
                    case ExifTag.GPSDestDistance:
                        break;
                    case ExifTag.GPSProcessingMethod:
                        break;
                    case ExifTag.GPSAreaInformation:
                        break;
                    case ExifTag.GPSDateStamp:
                        break;
                    case ExifTag.GPSDifferential:
                        break;
                    case ExifTag.InteroperabilityIndex:
                        break;
                    case ExifTag.InteroperabilityVersion:
                        break;
                    case ExifTag.RelatedImageWidth:
                        break;
                    case ExifTag.RelatedImageHeight:
                        break;
                    case ExifTag.ThumbnailImageWidth:
                        break;
                    case ExifTag.ThumbnailImageLength:
                        break;
                    case ExifTag.ThumbnailBitsPerSample:
                        break;
                    case ExifTag.ThumbnailCompression:
                        break;
                    case ExifTag.ThumbnailPhotometricInterpretation:
                        break;
                    case ExifTag.ThumbnailOrientation:
                        break;
                    case ExifTag.ThumbnailSamplesPerPixel:
                        break;
                    case ExifTag.ThumbnailPlanarConfiguration:
                        break;
                    case ExifTag.ThumbnailYCbCrSubSampling:
                        break;
                    case ExifTag.ThumbnailYCbCrPositioning:
                        break;
                    case ExifTag.ThumbnailXResolution:
                        break;
                    case ExifTag.ThumbnailYResolution:
                        break;
                    case ExifTag.ThumbnailResolutionUnit:
                        break;
                    case ExifTag.ThumbnailStripOffsets:
                        break;
                    case ExifTag.ThumbnailRowsPerStrip:
                        break;
                    case ExifTag.ThumbnailStripByteCounts:
                        break;
                    case ExifTag.ThumbnailJPEGInterchangeFormat:
                        break;
                    case ExifTag.ThumbnailJPEGInterchangeFormatLength:
                        break;
                    case ExifTag.ThumbnailTransferFunction:
                        break;
                    case ExifTag.ThumbnailWhitePoint:
                        break;
                    case ExifTag.ThumbnailPrimaryChromaticities:
                        break;
                    case ExifTag.ThumbnailYCbCrCoefficients:
                        break;
                    case ExifTag.ThumbnailReferenceBlackWhite:
                        break;
                    case ExifTag.ThumbnailDateTime:
                        break;
                    case ExifTag.ThumbnailImageDescription:
                        break;
                    case ExifTag.ThumbnailMake:
                        break;
                    case ExifTag.ThumbnailModel:
                        break;
                    case ExifTag.ThumbnailSoftware:
                        break;
                    case ExifTag.ThumbnailArtist:
                        break;
                    case ExifTag.ThumbnailCopyright:
                        break;
                    case ExifTag.JFIFVersion:
                        break;
                    case ExifTag.JFIFUnits:
                        break;
                    case ExifTag.XDensity:
                        break;
                    case ExifTag.YDensity:
                        break;
                    case ExifTag.JFIFXThumbnail:
                        break;
                    case ExifTag.JFIFYThumbnail:
                        break;
                    case ExifTag.JFIFThumbnail:
                        break;
                    case ExifTag.JFXXExtensionCode:
                        break;
                    case ExifTag.JFXXXThumbnail:
                        break;
                    case ExifTag.JFXXYThumbnail:
                        break;
                    case ExifTag.JFXXPalette:
                        break;
                    case ExifTag.JFXXThumbnail:
                        break;
                    case ExifTag.PNGTitle:
                        break;
                    case ExifTag.PNGAuthor:
                        break;
                    case ExifTag.PNGDescription:
                        break;
                    case ExifTag.PNGCopyright:
                        break;
                    case ExifTag.PNGCreationTime:
                        break;
                    case ExifTag.PNGSoftware:
                        break;
                    case ExifTag.PNGDisclaimer:
                        break;
                    case ExifTag.PNGWarning:
                        break;
                    case ExifTag.PNGSource:
                        break;
                    case ExifTag.PNGComment:
                        break;
                    case ExifTag.PNGText:
                        break;
                    case ExifTag.PNGTimeStamp:
                        break;
                    default:
                        break;
                }
            }

            return exifData;
        }
    }
}
