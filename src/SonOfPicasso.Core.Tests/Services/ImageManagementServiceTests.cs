using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class ImageManagementServiceTests : TestsBase<ImageManagementServiceTests>
    {
        public ImageManagementServiceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Timeout = 500)]
        public void CanInitialize()
        {
            Logger.LogDebug("CanInitialize");

            var imageLoadingService = this.CreateImageManagementService();
        }
    }
}