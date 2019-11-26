using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;

namespace SonOfPicasso.Data.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly Lazy<GenericRepository<AlbumImage>> _albumImageRepository;
        private readonly Lazy<GenericRepository<Album>> _albumRepository;
        private readonly DataContext _dataContext;
        private readonly Lazy<GenericRepository<Folder>> _directoryRepository;
        private readonly Lazy<GenericRepository<ExifData>> _exifDataRepository;
        private readonly Lazy<GenericRepository<FolderRule>> _folderRuleRepository;
        private readonly Lazy<GenericRepository<Image>> _imageRepository;
        internal bool Disposed;

        public UnitOfWork(DbContextOptions<DataContext> dataContextOptions)
        {
            _dataContext = new DataContext(dataContextOptions);
            _albumRepository = new Lazy<GenericRepository<Album>>(() => new GenericRepository<Album>(_dataContext));
            _exifDataRepository =
                new Lazy<GenericRepository<ExifData>>(() => new GenericRepository<ExifData>(_dataContext));
            _imageRepository = new Lazy<GenericRepository<Image>>(() => new GenericRepository<Image>(_dataContext));
            _directoryRepository =
                new Lazy<GenericRepository<Folder>>(() => new GenericRepository<Folder>(_dataContext));
            _albumImageRepository =
                new Lazy<GenericRepository<AlbumImage>>(() => new GenericRepository<AlbumImage>(_dataContext));
            _folderRuleRepository =
                new Lazy<GenericRepository<FolderRule>>(() => new GenericRepository<FolderRule>(_dataContext));
        }

        public IGenericRepository<Album> AlbumRepository => _albumRepository.Value;
        public IGenericRepository<Image> ImageRepository => _imageRepository.Value;
        public IGenericRepository<Folder> FolderRepository => _directoryRepository.Value;
        public IGenericRepository<AlbumImage> AlbumImageRepository => _albumImageRepository.Value;
        public IGenericRepository<FolderRule> FolderRuleRepository => _folderRuleRepository.Value;
        public IGenericRepository<ExifData> ExifDataRepository => _exifDataRepository.Value;

        public void Save()
        {
            _dataContext.SaveChanges();
        }

        public IDbContextTransaction BeginTransaction()
        {
            return _dataContext.Database.BeginTransaction();
        }

        public void Dispose()
        {
            _dataContext?.Dispose();
        }
    }
}