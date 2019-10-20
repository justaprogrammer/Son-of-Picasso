using FluentAssertions;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.Services
{
    public class SvgLoadingServiceTests : UnitTestsBase
    {
        public SvgLoadingServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanLoadBitmapFromResource()
        {
            var flatIconSvgLoader = new SvgImageService();
            var image = flatIconSvgLoader.LoadBitmap("FlatColorIcons.folder");
            image.Height.Should().Be(48);
            image.Width.Should().Be(48);
        }

        [Fact]
        public void CanLoadBitmapImageFromResource()
        {
            var flatIconSvgLoader = new SvgImageService();
            var image = flatIconSvgLoader.LoadBitmapImage("FlatColorIcons.folder");
            image.Height.Should().Be(48);
            image.Width.Should().Be(48);
        }
    }
}