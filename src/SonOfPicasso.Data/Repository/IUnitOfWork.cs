using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Data.Repository
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Album> AlbumRepository { get; }
        IGenericRepository<Image> ImageRepository { get; }
        IGenericRepository<Folder> FolderRepository { get; }
        IGenericRepository<AlbumImage> AlbumImageRepository { get; }
        void Save();
    }
}