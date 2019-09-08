using System;
using Microsoft.EntityFrameworkCore;
using SonOfPicasso.Data.Context;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Data.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDataContext _dataContext;

        private readonly Lazy<GenericRepository<Album>> _albumRepository;
        public IGenericRepository<Album> AlbumRepository => _albumRepository.Value;

        private readonly Lazy<GenericRepository<Image>> _imageRepository;
        public IGenericRepository<Image> ImageRepository => _imageRepository.Value;

        private readonly Lazy<GenericRepository<Directory>> _directoryRepository;
        public IGenericRepository<Directory> DirectoryRepository => _directoryRepository.Value;

        private readonly Lazy<GenericRepository<AlbumImage>> _albumImageRepository;
        public IGenericRepository<AlbumImage> AlbumImageRepository => _albumImageRepository.Value;

        public void Save()
        {
            _dataContext.SaveChanges();
        }

        public UnitOfWork(DbContextOptions<DataContext> dataContextOptions)
        {
            _dataContext = new DataContext(dataContextOptions);
            _albumRepository = new Lazy<GenericRepository<Album>>(() => new GenericRepository<Album>(_dataContext));
            _imageRepository = new Lazy<GenericRepository<Image>>(() => new GenericRepository<Image>(_dataContext));
            _directoryRepository = new Lazy<GenericRepository<Directory>>(() => new GenericRepository<Directory>(_dataContext));
            _albumImageRepository = new Lazy<GenericRepository<AlbumImage>>(() => new GenericRepository<AlbumImage>(_dataContext));
        }

        internal bool Disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    _dataContext.Dispose();
                }
            }
            Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}