using FluentAssertions;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using SonOfPicasso.UI.Extensions;
using SonOfPicasso.UI.Services;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.UI.Tests.Extensions
{
    public class BitmapExtensionTests : UnitTestsBase
    {
        public BitmapExtensionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        [Fact]
        public void CanLoadBitmapImageFromResource()
        {
            var flatIconSvgLoader = new SvgLoadingService();
            
            using var bitmap = flatIconSvgLoader.Load("SonOfPicasso.UI.Resources.FlatColorIcons.folder.svg", typeof(ImageProvider));
            var bitmapImage = bitmap.ToBitmapImage();

            bitmapImage.Height.Should().BeInRange(48, 48.01);
            bitmapImage.Width.Should().BeInRange(48, 48.01);
        }
    }
}