using Akavache;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Services;

namespace SonOfPicasso.Testing.Common.Services
{
    public class TestCache : DataCache
    {
        public TestCache(ILogger<DataCache> logger) : base(logger, new InMemoryBlobCache())
        {
        }

        public InMemoryBlobCache InMemoryBlobCache => (InMemoryBlobCache) BlobCache;
    }
}