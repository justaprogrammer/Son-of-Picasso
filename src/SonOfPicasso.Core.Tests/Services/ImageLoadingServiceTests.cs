using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageLoadingServiceTests : TestsBase<ImageLoadingServiceTests>
    {
        public ImageLoadingServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.LogDebug("CanInitialize");

            var imageLoadingService = this.CreateImageLoadingService();
        }
    }
}