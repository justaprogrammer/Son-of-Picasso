using System;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;

namespace SonOfPicasso.Data.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _dataContext;
        private readonly Lazy<GenericRepository<AlbumImage>> _albumImageRepository;
        private readonly Lazy<GenericRepository<Album>> _albumRepository;
        private readonly Lazy<GenericRepository<Folder>> _directoryRepository;
        private readonly Lazy<GenericRepository<Image>> _imageRepository;
        internal bool Disposed;

        public UnitOfWork(DbContextOptions<DataContext> dataContextOptions)
        {
            _dataContext = new DataContext(dataContextOptions);
            _albumRepository = new Lazy<GenericRepository<Album>>(() => new GenericRepository<Album>(_dataContext));
            _imageRepository = new Lazy<GenericRepository<Image>>(() => new GenericRepository<Image>(_dataContext));
            _directoryRepository =
                new Lazy<GenericRepository<Folder>>(() => new GenericRepository<Folder>(_dataContext));
            _albumImageRepository =
                new Lazy<GenericRepository<AlbumImage>>(() => new GenericRepository<AlbumImage>(_dataContext));
        }

        public IGenericRepository<Album> AlbumRepository => _albumRepository.Value;
        public IGenericRepository<Image> ImageRepository => _imageRepository.Value;
        public IGenericRepository<Folder> FolderRepository => _directoryRepository.Value;
        public IGenericRepository<AlbumImage> AlbumImageRepository => _albumImageRepository.Value;

        public T WithContext<T>(Func<IDataContext, T> func)
        {
            return func(_dataContext);
        }

        public void Save()
        {
            _dataContext.SaveChanges();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
                if (disposing)
                    _dataContext.Dispose();
            Disposed = true;
        }
    }
}