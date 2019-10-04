using System;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;

namespace SonOfPicasso.Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Album> AlbumRepository { get; }
        IGenericRepository<Image> ImageRepository { get; }
        IGenericRepository<Folder> FolderRepository { get; }
        IGenericRepository<AlbumImage> AlbumImageRepository { get; }
        void Save();
        T WithContext<T>(Func<IDataContext, T> func);
    }
}