using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.Testing.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ExifDataServiceTests : TestsBase<ExifDataServiceTests>
    {
        public ExifDataServiceTests(ITestOutputHelper testOutputHelper) 
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.LogDebug("CanInitialize");

            var exifDataService = this.CreateExifDataService();
        }

        [Fact]
        public void CanReadExifData()
        {
            Logger.LogDebug("CanReadExifData");

            var mockFileSystem = new MockFileSystem();
            var resourceAssembly = Assembly.GetAssembly(typeof(TestsBase<>));

            var filePath = Path.Combine(Faker.System.DirectoryPathWindows(), Faker.System.FileName("jpg"));
            mockFileSystem.AddFileFromEmbeddedResource(filePath, resourceAssembly, "SonOfPicasso.Testing.Common.Resources.DSC04085.JPG");

            var exifDataService = this.CreateExifDataService(mockFileSystem);
            var exifData = exifDataService.GetExifData(filePath);

            exifData.Make.Should().Be("SONY");
            exifData.Model.Should().Be("DSC-RX100M3");
            exifData.UserComment.Should().Be(String.Empty);
            exifData.FileSource.Should().Be("DSC");
            exifData.Software.Should().Be("DSC-RX100M3 v1.20");
        }
    }
}