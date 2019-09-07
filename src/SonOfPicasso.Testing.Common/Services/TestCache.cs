using Akavache;
using Serilog;
using SonOfPicasso.Core.Services;

namespace SonOfPicasso.Testing.Common.Services
{
    public class TestCache : DataCache
    {
        public TestCache(ILogger logger) : base(logger, new InMemoryBlobCache())
        {
        }

        public InMemoryBlobCache InMemoryBlobCache => (InMemoryBlobCache) BlobCache;
    }
}