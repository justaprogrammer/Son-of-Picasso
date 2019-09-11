using System;

namespace SonOfPicasso.Data.Model
{
    public class ExifData
    {
        public int Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Software { get; set; }
        public string UserComment { get; set; }
        public string FileSource { get; set; }
        public string ImageDescription { get; set; }
        public string DocumentName { get; set; }
        public string Orientation { get; set; }
        public string XResolution { get; set; }
        public string YResolution { get; set; }
        public string ThumbnailXResolution { get; set; }
        public string ThumbnailYResolution { get; set; }
        public string ExposureTime { get; set; }
        public string CompressedBitsPerPixel { get; set; }
        public string FocalLength { get; set; }
        public string ThumbnailImageDescription { get; set; }
        public string ThumbnailMake { get; set; }
        public string ThumbnailModel { get; set; }
        public string ThumbnailSoftware { get; set; }
        public string InteroperabilityIndex { get; set; }
        public uint PixelXDimension { get; set; }
        public uint PixelYDimension { get; set; }
        public uint InteroperabilityIFDPointer { get; set; }
        public uint ThumbnailJPEGInterchangeFormat { get; set; }
        public uint ThumbnailJPEGInterchangeFormatLength { get; set; }
        public DateTime DateTime { get; set; }
        public uint EXIFIFDPointer { get; set; }
        public DateTime DateTimeDigitized { get; set; }
        public DateTime DateTimeOriginal { get; set; }
        public string FNumber { get; set; }
        public string MaxApertureValue { get; set; }
        public string DigitalZoomRatio { get; set; }
        public DateTime ThumbnailDateTime { get; set; }
        public ushort ISOSpeedRatings { get; set; }
        // ReSharper disable once InconsistentNaming
        public ushort FocalLengthIn35mmFilm { get; set; }
        public string ColorSpace { get; set; }
        public string ExposureMode { get; set; }
        public string MeteringMode { get; set; }
        public string LightSource { get; set; }
        public string SceneCaptureType { get; set; }
        public string ResolutionUnit { get; set; }
        public string YCbCrPositioning { get; set; }
        public string ExposureProgram { get; set; }
        public string Flash { get; set; }
        public string SceneType { get; set; }
        public string CustomRendered { get; set; }
        public string WhiteBalance { get; set; }
        public string Contrast { get; set; }
        public string Saturation { get; set; }
        public string Sharpness { get; set; }
        public string ThumbnailCompression { get; set; }
        public string ThumbnailOrientation { get; set; }
        public string ThumbnailResolutionUnit { get; set; }
        public string ThumbnailYCbCrPositioning { get; set; }
        public string ExifVersion { get; set; }
        public string FlashpixVersion { get; set; }
        public string InteroperabilityVersion { get; set; }
        public string BrightnessValue { get; set; }
        public string ExposureBiasValue { get; set; }
        public string LensSpecification { get; set; }
    }
}