using Akavache;
using Microsoft.Extensions.Logging;
using SonOfPicasso.Core.Services;

namespace SonOfPicasso.Testing.Common.Services
{
    public class TestCache : SharedCache
    {
        public TestCache(ILogger<SharedCache> logger) : base(logger, new InMemoryBlobCache(), new InMemoryBlobCache())
        {
        }
    }
}