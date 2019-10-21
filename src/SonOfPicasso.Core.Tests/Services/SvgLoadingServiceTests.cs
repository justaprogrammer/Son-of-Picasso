using System.Reflection;
using FluentAssertions;
using SonOfPicasso.Core.Services;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class SvgLoadingServiceTests : UnitTestsBase
    {
        public SvgLoadingServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanLoadBitmapFromResource()
        {
            var flatIconSvgLoader = new SvgLoadingService();
            var image = flatIconSvgLoader.Load("SonOfPicasso.Core.Tests.Resources.FlatColorIcons.folder.svg", typeof(SvgLoadingServiceTests));
            image.Height.Should().Be(48);
            image.Width.Should().Be(48);
        }
    }
}