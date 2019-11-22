using Akavache;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.Core.Services
{
    public class BlobCacheProvider : IBlobCacheProvider
    {
        private ISecureBlobCache _inMemory;
        private IBlobCache _localMachine;
        private ISecureBlobCache _secure;
        private IBlobCache _userAccount;
        public IBlobCache UserAccount => _userAccount ??= BlobCache.UserAccount;
        public ISecureBlobCache InMemory => _inMemory ??= BlobCache.InMemory;
        public IBlobCache LocalMachine => _localMachine ??= BlobCache.LocalMachine;
        public ISecureBlobCache Secure => _secure ??= BlobCache.Secure;
    }
}