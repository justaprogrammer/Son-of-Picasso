using System.IO.Abstractions;
using Akavache;
using Akavache.Sqlite3;
using SonOfPicasso.Core.Interfaces;

namespace SonOfPicasso.Core.Services
{
    public class CustomBlobCacheProvider : IBlobCacheProvider
    {
        public CustomBlobCacheProvider(IFileSystem fileSystem, string cachePath)
        {
            var directoryInfo = fileSystem.Directory.CreateDirectory(cachePath);
            UserAccount = new SQLiteEncryptedBlobCache(fileSystem.Path.Combine(directoryInfo.FullName, "UserAccount.db"));
            InMemory = new InMemoryBlobCache();
            LocalMachine = new SQLiteEncryptedBlobCache(fileSystem.Path.Combine(directoryInfo.FullName, "LocalMachine.db"));
            Secure = new SQLiteEncryptedBlobCache(fileSystem.Path.Combine(directoryInfo.FullName, "Secure.db"));
        }

        public IBlobCache UserAccount { get; }

        public ISecureBlobCache InMemory { get; }

        public IBlobCache LocalMachine { get; }

        public ISecureBlobCache Secure { get; }
    }
}