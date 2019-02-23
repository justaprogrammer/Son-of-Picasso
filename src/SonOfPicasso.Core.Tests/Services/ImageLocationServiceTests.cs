using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageLocationServiceTests : TestsBase<ImageLocationServiceTests>
    {
        public ImageLocationServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.LogDebug("CanInitialize");

            var imageLocationService = this.CreateImageLocationService();
        }
    }
}