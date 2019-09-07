using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Data
{
    public class UnitOfWork : IDisposable
    {
        private readonly IDataContext _context;

        private readonly Lazy<GenericRepository<Album>> _albumRepository;
        public GenericRepository<Album> AlbumRepository => _albumRepository.Value;

        private readonly Lazy<GenericRepository<Image>> _imageRepository;
        public GenericRepository<Image> ImageRepository => _imageRepository.Value;

        private readonly Lazy<GenericRepository<Directory>> _directoryRepository;
        public GenericRepository<Directory> DirectoryRepository => _directoryRepository.Value;

        private readonly Lazy<GenericRepository<AlbumImage>> _albumImageRepository;
        public GenericRepository<AlbumImage> AlbumImageRepository => _albumImageRepository.Value;

        public void Save()
        {
            _context.SaveChanges();
        }

        public UnitOfWork(IDataContext dataContext)
        {
            _context = dataContext;
            _albumRepository = new Lazy<GenericRepository<Album>>(() => new GenericRepository<Album>(_context));
            _imageRepository = new Lazy<GenericRepository<Image>>(() => new GenericRepository<Image>(_context));
            _directoryRepository = new Lazy<GenericRepository<Directory>>(() => new GenericRepository<Directory>(_context));
            _albumImageRepository = new Lazy<GenericRepository<AlbumImage>>(() => new GenericRepository<AlbumImage>(_context));
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}