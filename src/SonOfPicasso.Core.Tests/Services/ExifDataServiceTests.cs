using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Threading;
using Autofac;
using Autofac.Builder;
using Autofac.Core.Registration;
using Autofac.Extras.NSubstitute;
using AutofacSerilogIntegration;
using ExifLibrary;
using FluentAssertions;
using FluentAssertions.Execution;
using Serilog;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using SonOfPicasso.Testing.Common.Scheduling;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ExifDataServiceTests : TestsBase, IDisposable
    {
        private readonly AutoSubstitute _autoSubstitute;
        private readonly MockFileSystem _mockFileSystem;
        private readonly AutoResetEvent _autoResetEvent;
        private readonly TestSchedulerProvider _testSchedulerProvider;

        public ExifDataServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            var builder = new ContainerBuilder();
            builder.RegisterLogger();

            _testSchedulerProvider = new TestSchedulerProvider();
            builder.RegisterInstance(_testSchedulerProvider).As<ISchedulerProvider>();

            _mockFileSystem = new MockFileSystem();
            builder.RegisterInstance(_mockFileSystem).As<IFileSystem>();

            _autoSubstitute = new AutoSubstitute(builder);
            _autoResetEvent = new AutoResetEvent(false);
        }

        public void Dispose()
        {
            _autoSubstitute.Dispose();
            _autoResetEvent.Dispose();
        }

        [Fact]
        public void CanReadExifData()
        {
            Logger.Debug("CanReadExifData");

            var resourceAssembly = Assembly.GetAssembly(typeof(TestsBase));

            var filePath = Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("jpg"));
            _mockFileSystem.AddFileFromEmbeddedResource(filePath, resourceAssembly, "SonOfPicasso.Testing.Common.Resources.DSC04085.JPG");

            var exifDataService = _autoSubstitute.Resolve<ExifDataService>();

            ExifData exifData = null;
            exifDataService.GetExifData(filePath)
                .Subscribe(data =>
                {
                    exifData = data;
                    _autoResetEvent.Set();
                });
            
            _testSchedulerProvider.TaskPool.AdvanceBy(1);

            _autoResetEvent.WaitOne(1000).Should().BeTrue();

            exifData.Should().NotBeNull();

            using (new AssertionScope())
            {
                var blankString = new String(' ', 31);
                exifData.Make.Should().Be("SONY");
                exifData.Model.Should().Be("DSC-RX100M3");
                exifData.XResolution.Should().Be("350/1");
                exifData.YResolution.Should().Be("350/1");
                exifData.DocumentName.Should().BeNull();
                exifData.ImageDescription.Should().Be(blankString);
                exifData.UserComment.Should().Be(string.Empty);
                exifData.FileSource.Should().Be("DSC");
                exifData.Software.Should().Be("DSC-RX100M3 v1.20");
                exifData.Orientation.Should().Be("Normal");
                exifData.ThumbnailXResolution.Should().Be("72/1");
                exifData.ThumbnailYResolution.Should().Be("72/1");
                exifData.ExposureTime.Should().Be("1/320");
                exifData.CompressedBitsPerPixel.Should().Be("2/1");
                exifData.FocalLength.Should().Be("257/10");
                exifData.ThumbnailImageDescription.Should().Be(blankString);
                exifData.ThumbnailMake.Should().Be("SONY");
                exifData.ThumbnailModel.Should().Be("DSC-RX100M3");
                exifData.ThumbnailSoftware.Should().Be("DSC-RX100M3 v1.20");
                exifData.InteroperabilityIndex.Should().Be("R98");
                exifData.MeteringMode.Should().Be("Pattern");
                exifData.LightSource.Should().Be("Unknown");
                exifData.SceneCaptureType.Should().Be("Standard");
                exifData.ColorSpace.Should().Be("sRGB");
                exifData.ExposureMode.Should().Be("Auto");
                exifData.ResolutionUnit.Should().Be("Inches");
                exifData.YCbCrPositioning.Should().Be("CoSited");
                exifData.ExposureProgram.Should().Be("Normal");
                exifData.Flash.Should().Be("AutoMode");
                exifData.SceneType.Should().Be("DirectlyPhotographedImage");
                exifData.CustomRendered.Should().Be("NormalProcess");
                exifData.WhiteBalance.Should().Be("Auto");
                exifData.Contrast.Should().Be("Normal");
                exifData.Saturation.Should().Be("Normal");
                exifData.Sharpness.Should().Be("Normal");
                exifData.ThumbnailCompression.Should().Be("JPEG");
                exifData.ThumbnailOrientation.Should().Be("Normal");
                exifData.ThumbnailResolutionUnit.Should().Be("Inches");
                exifData.ThumbnailYCbCrPositioning.Should().Be("CoSited");
                exifData.FocalLengthIn35mmFilm.Should().Be(70);
                exifData.ISOSpeedRatings.Should().Be(125);
                exifData.PixelXDimension.Should().Be(4864);
                exifData.PixelYDimension.Should().Be(3648);
                exifData.InteroperabilityIFDPointer.Should().Be(31652);
                exifData.ThumbnailJPEGInterchangeFormat.Should().Be(31948);
                exifData.ThumbnailJPEGInterchangeFormatLength.Should().Be(12240);
                exifData.DateTime.Should().BeCloseTo(DateTime.Parse("2018-02-08 04:31:33"));
                exifData.DateTimeDigitized.Should().BeCloseTo(DateTime.Parse("2018-02-08 04:31:33"));
                exifData.DateTimeOriginal.Should().BeCloseTo(DateTime.Parse("2018-02-08 04:31:33"));
                exifData.ThumbnailDateTime.Should().BeCloseTo(DateTime.Parse("2018-02-08 04:31:33"));
                exifData.EXIFIFDPointer.Should().Be(290);
                exifData.FNumber.Should().Be("4/1");
                exifData.DigitalZoomRatio.Should().Be("2/1");
                exifData.BrightnessValue.Should().Be("657/80");
                exifData.ExposureBiasValue.Should().Be("0/1");
                exifData.ExifVersion.Should().Be("0230");
                exifData.FlashpixVersion.Should().Be("0100");
                exifData.InteroperabilityVersion.Should().Be("0100");
                exifData.LensSpecification.Should().Be("44/5 F9/5, 257/10 F14/5");
            }
        }
    }
}