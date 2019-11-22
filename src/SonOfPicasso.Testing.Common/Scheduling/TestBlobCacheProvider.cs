using Akavache;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.Testing.Common.Scheduling
{
    public class TestBlobCacheProvider : IBlobCacheProvider
    {
        private InMemoryBlobCache _userAccount;
        private InMemoryBlobCache _inMemory;
        private InMemoryBlobCache _localMachine;
        private InMemoryBlobCache _secure;

        public IBlobCache UserAccount => _userAccount ??= new InMemoryBlobCache();

        public ISecureBlobCache InMemory => _inMemory ??= new InMemoryBlobCache();

        public IBlobCache LocalMachine => _localMachine ??= new InMemoryBlobCache();

        public ISecureBlobCache Secure => _secure ??= new InMemoryBlobCache();
    }
}