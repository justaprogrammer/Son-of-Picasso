using Akavache;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IBlobCacheProvider
    {
        IBlobCache UserAccount { get; }
        ISecureBlobCache InMemory { get; }
        IBlobCache LocalMachine { get; }
        ISecureBlobCache Secure { get; }
    }
}