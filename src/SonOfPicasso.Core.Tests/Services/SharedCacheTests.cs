using Akavache;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Tests.Extensions;
using SonOfPicasso.Testing.Common;
using Xunit;
using Xunit.Abstractions;

namespace SonOfPicasso.Core.Tests.Services
{
    public class SharedCacheTests : TestsBase<SharedCacheTests>
    {
        public SharedCacheTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanInitialize()
        {
            Logger.LogDebug("CanInitialize");

            var inMemoryBlobCache = new InMemoryBlobCache();
            var sharedCache = this.CreateSharedCache(inMemoryBlobCache);
        }
    }
}
