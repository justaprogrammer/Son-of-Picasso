using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using Autofac.Extras.NSubstitute;
using FluentAssertions;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ExifDataServiceTests : TestsBase, IDisposable
    {
        private readonly AutoSubstitute _autoSubstitute;
        private readonly MockFileSystem _mockFileSystem;

        public ExifDataServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            _autoSubstitute = new AutoSubstitute();
            _mockFileSystem = new MockFileSystem();
            _autoSubstitute.Provide<IFileSystem>(_mockFileSystem);
        }

        public void Dispose()
        {
            _autoSubstitute.Dispose();
        }

        [Fact]
        public void CanReadExifData()
        {
            Logger.Debug("CanReadExifData");

            var resourceAssembly = Assembly.GetAssembly(typeof(TestsBase));

            var filePath = Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("jpg"));
            _mockFileSystem.AddFileFromEmbeddedResource(filePath, resourceAssembly, "SonOfPicasso.Testing.Common.Resources.DSC04085.JPG");

            var exifDataService = _autoSubstitute.Resolve<ExifDataService>();

            var exifData = exifDataService.GetExifData(filePath);

            exifData.Make.Should().Be("SONY");
            exifData.Model.Should().Be("DSC-RX100M3");
            exifData.DocumentName.Should().Be(new String(' ', 31));
            exifData.ImageDescription.Should().Be(new String(' ', 31));
            exifData.UserComment.Should().Be(string.Empty);
            exifData.FileSource.Should().Be("DSC");
            exifData.Software.Should().Be("DSC-RX100M3 v1.20");
        }
    }
}