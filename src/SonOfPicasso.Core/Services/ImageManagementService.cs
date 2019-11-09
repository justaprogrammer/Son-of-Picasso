using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Interfaces;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private const string AlbumDefaultProperties = "AlbumImages,AlbumImages.Image,AlbumImages.Image.ExifData";
        private const string FolderDefaultProperties = "Images,Images.ExifData";

        private readonly IExifDataService _exifDataService;
        private readonly IFileSystem _fileSystem;
        private readonly IImageLocationService _imageLocationService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly Func<IUnitOfWork> _unitOfWorkFactory;
        private readonly object _writeLock = new object();

        public ImageManagementService(IFileSystem fileSystem,
            ILogger logger,
            IImageLocationService imageLocationService,
            Func<IUnitOfWork> unitOfWorkFactory,
            ISchedulerProvider schedulerProvider,
            IExifDataService exifDataService)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _imageLocationService = imageLocationService;
            _unitOfWorkFactory = unitOfWorkFactory;
            _schedulerProvider = schedulerProvider;
            _exifDataService = exifDataService;
        }

        public IObservable<IImageContainer> ScanFolder(string path)
        {
            return Observable.Defer(() =>
                {
                    _logger.Debug("ScanFolder {Path}", path);

                    using var unitOfWork = _unitOfWorkFactory();

                    var imagesAtPathHashSet = unitOfWork.ImageRepository
                        .Get(image => image.Path.StartsWith(path))
                        .Select(image => image.Path)
                        .ToHashSet();

                    return Observable.Return(imagesAtPathHashSet);
                })
                .SelectMany(imagesAtPath =>
                {
                    return _imageLocationService
                        .GetImages(path)
                        .Where(imagePath => !imagesAtPath.Contains(imagePath))
                        .GroupBy(s => _fileSystem.Path.GetDirectoryName(s))
                        .SelectMany(async groupedObservable =>
                        {
                            var images = await groupedObservable
                                .SelectMany(imagePath => _exifDataService
                                    .GetExifData(imagePath)
                                    .Select(exifData => new Image
                                    {
                                        Path = imagePath,
                                        ExifData = exifData
                                    }))
                                .ToArray();

                            var minDate = images.Select(image => image.ExifData.DateTime).Min();

                            var folderPath = groupedObservable.Key;

                            lock (_writeLock)
                            {
                                using var unitOfWork = _unitOfWorkFactory();
                                using var transaction = unitOfWork.BeginTransaction();

                                var folder = unitOfWork.FolderRepository
                                    .Get(d => d.Path == folderPath, includeProperties: FolderDefaultProperties)
                                    .FirstOrDefault();

                                if (folder != null)
                                {
                                    folder.Images.AddRange(images);
                                }
                                else
                                {
                                    folder = new Folder
                                    {
                                        Path = folderPath,
                                        Images = images.ToList(),
                                        Date = minDate.Date
                                    };

                                    unitOfWork.FolderRepository.Insert(folder);
                                }

                                unitOfWork.Save();
                                transaction.Commit();
                                return folder.Id;
                            }

                        })
                        .SelectMany(GetFolderImageContainer);
                }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> CreateAlbum(ICreateAlbum createAlbum)
        {
            return Observable.Defer(() =>
                {
                    lock (_writeLock)
                    {
                        using var unitOfWork = _unitOfWorkFactory();
                        using var transaction = unitOfWork.BeginTransaction();

                        var album = new Album
                        {
                            Name = createAlbum.AlbumName,
                            Date = createAlbum.AlbumDate,
                            AlbumImages = new List<AlbumImage>()
                        };

                        unitOfWork.AlbumRepository.Insert(album);
                        unitOfWork.Save();
                        transaction.Commit();
                        return Observable.Return(album);
                    }
                })
                .Select(album => (IImageContainer)new AlbumImageContainer(album))
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> GetAllImageContainers()
        {
            var selectFolders = Observable.Create<IImageContainer>(observer =>
            {
                var unitOfWork = _unitOfWorkFactory();

                var folders = unitOfWork.FolderRepository
                    .Get(includeProperties: FolderDefaultProperties)
                    .ToArray();

                foreach (var folder in folders) observer.OnNext(new FolderImageContainer(folder, _fileSystem));

                observer.OnCompleted();

                return unitOfWork;
            });

            var selectAlbums = Observable.Create<IImageContainer>(observer =>
            {
                var unitOfWork = _unitOfWorkFactory();

                var albums = unitOfWork.AlbumRepository
                    .Get(includeProperties: AlbumDefaultProperties)
                    .ToArray();

                foreach (var album in albums) observer.OnNext(new AlbumImageContainer(album));

                observer.OnCompleted();

                return unitOfWork;
            });

            return selectFolders.Merge(selectAlbums)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> AddImagesToAlbum(int albumId, IEnumerable<int> imageIds)
        {
            return Observable.Defer(() =>
                {
                    lock (_writeLock)
                    {
                        using var unitOfWork = _unitOfWorkFactory();
                        var transaction = unitOfWork.BeginTransaction();

                        var imageIdHash = unitOfWork.AlbumImageRepository.Get(image => image.AlbumId == albumId)
                            .Select(image => image.ImageId)
                            .ToHashSet();

                        foreach (var imageId in imageIds)
                            if (!imageIdHash.Contains(imageId))
                                unitOfWork.AlbumImageRepository.Insert(new AlbumImage
                                {
                                    ImageId = imageId,
                                    AlbumId = albumId
                                });

                        unitOfWork.Save();
                        transaction.Commit();

                        return Observable.Return(albumId);
                    }
                })
                .SelectMany(GetAlbumImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> AddImage(string path)
        {
            return Observable.DeferAsync(async token =>
                {
                    var exifData = await _exifDataService.GetExifData(path);

                    var directory = _fileSystem.Path.GetDirectoryName(path);
                    lock (_writeLock)
                    {
                        _logger.Debug("AddImage {Path}", path);

                        using var unitOfWork = _unitOfWorkFactory();
                        using var transaction = unitOfWork.BeginTransaction();

                        var image = unitOfWork.ImageRepository
                            .Get(i => i.Path.Equals(path), includeProperties: "Folder")
                            .FirstOrDefault();

                        if (image != null)
                        {
                            transaction.Rollback();
                            return Observable.Empty<int>();
                        }

                        image = new Image
                        {
                            Path = path,
                            ExifData = exifData
                        };

                        var folder = unitOfWork.FolderRepository
                            .Get(f => f.Path.Equals(directory))
                            .FirstOrDefault();

                        if (folder != null)
                        {
                            folder.Images ??= new List<Image>();
                            folder.Images.Add(image);
                        }
                        else
                        {
                            folder = new Folder
                            {
                                Path = directory,
                                Date = exifData.DateTime.Date,
                                Images = new List<Image> { image }
                            };

                            unitOfWork.FolderRepository.Insert(folder);
                        }

                        unitOfWork.Save();
                        transaction.Commit();
                        return Observable.Return(folder.Id);
                    }
                })
                .SelectMany(GetFolderImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> DeleteImage(string path)
        {
            return Observable.Defer(() =>
                {
                    lock (_writeLock)
                    {
                        using var unitOfWork = _unitOfWorkFactory();
                        using var transaction = unitOfWork.BeginTransaction();
                        var image = unitOfWork.ImageRepository
                            .Get(i => i.Path.Equals(path))
                            .FirstOrDefault();

                        if (image == null)
                            return Observable.Empty<int>();

                        var imageFolderId = image.FolderId;
                        unitOfWork.ImageRepository.Delete(image);
                        unitOfWork.ExifDataRepository.Delete(image.ExifDataId);
                        unitOfWork.Save();
                        transaction.Commit();

                        return Observable.Return(imageFolderId);
                    }
                })
                .SelectMany(GetFolderImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> UpdateImage(string path)
        {
            return Observable.DeferAsync(async token =>
                {
                    var exifData = await _exifDataService.GetExifData(path);

                    lock (_writeLock)
                    {
                        _logger.Debug("UpdateImage {Path}", path);

                        using var unitOfWork = _unitOfWorkFactory();
                        using var transaction = unitOfWork.BeginTransaction();

                        var image = unitOfWork.ImageRepository
                            .Get(i => i.Path.Equals(path))
                            .First();


                        unitOfWork.ExifDataRepository.Update(exifData);
                        unitOfWork.Save();
                        transaction.Commit();
                        return Observable.Return(image.FolderId);
                    }
                })
                .SelectMany(GetFolderImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> RenameImage(string oldPath, string newPath)
        {
            return Observable.Defer(() =>
                {
                    var oldDirectoryPath = _fileSystem.Path.GetDirectoryName(oldPath);
                    var newDirectoryPath = _fileSystem.Path.GetDirectoryName(newPath);

                    lock (_writeLock)
                    {
                        using var unitOfWork = _unitOfWorkFactory();
                        using var transaction = unitOfWork.BeginTransaction();

                        var image = unitOfWork.ImageRepository
                            .Get(i => i.Path.Equals(oldPath), includeProperties: "ExifData")
                            .First();

                        image.Path = newPath;

                        var oldFolderId = image.FolderId;

                        if (oldDirectoryPath == newDirectoryPath)
                        {
                            unitOfWork.Save();
                            return Observable.Return(image.FolderId);
                        }

                        var folder = unitOfWork.FolderRepository
                            .Get(f => f.Path.Equals(newDirectoryPath))
                            .FirstOrDefault();

                        if (folder == null)
                        {
                            folder = new Folder
                            {
                                Path = newDirectoryPath,
                                Date = image.ExifData.DateTime.Date,
                                Images = new List<Image> { image }
                            };

                            unitOfWork.FolderRepository.Insert(folder);
                        }
                        else
                        {
                            image.FolderId = folder.Id;
                        }

                        unitOfWork.Save();
                        transaction.Commit();
                        return new[] { oldFolderId, folder.Id }.ToObservable();
                    }
                })
                .SelectMany(GetFolderImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Unit> DeleteAlbum(int albumId)
        {
            return Observable.Defer(() =>
                {
                    lock (_writeLock)
                    {
                        using var unitOfWork = _unitOfWorkFactory();
                        using var transaction = unitOfWork.BeginTransaction();

                        unitOfWork.AlbumRepository.Delete(albumId);
                        unitOfWork.Save();
                        transaction.Commit();
                        return Observable.Return(Unit.Default);
                    }
                })
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> AddOrUpdateImage(string path)
        {
            return Observable.DeferAsync(async token =>
                {
                    var exifData = await _exifDataService.GetExifData(path);
                    var directory = _fileSystem.Path.GetDirectoryName(path);

                    lock (_writeLock)
                    {
                        _logger.Debug("AddOrUpdateImage {Path}", path);

                        using var unitOfWork = _unitOfWorkFactory();
                        using var transaction = unitOfWork.BeginTransaction();

                        var image = unitOfWork.ImageRepository
                            .Get(i => i.Path.Equals(path))
                            .FirstOrDefault();

                        if (image == null)
                        {
                            image = new Image
                            {
                                Path = path,
                                ExifData = exifData
                            };

                            unitOfWork.ImageRepository.Insert(image);
                        }
                        else
                        {
                            exifData.Id = image.ExifDataId;
                            unitOfWork.ExifDataRepository.Update(exifData);
                        }

                        var folder = unitOfWork.FolderRepository
                            .Get(f => f.Path.Equals(directory))
                            .FirstOrDefault();

                        if (folder != null)
                        {
                            folder.Images ??= new List<Image>();
                            folder.Images.Add(image);
                        }
                        else
                        {
                            folder = new Folder
                            {
                                Path = directory,
                                Date = exifData.DateTime.Date,
                                Images = new List<Image> { image }
                            };

                            unitOfWork.FolderRepository.Insert(folder);
                        }

                        unitOfWork.Save();
                        transaction.Commit();
                    return Observable.Return(image.FolderId);
                    }
                })
                .SelectMany(GetFolderImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        private IObservable<IImageContainer> GetFolderImageContainer(int folderId)
        {
            return Observable.Defer<IImageContainer>(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                var folder = unitOfWork.FolderRepository
                    .Get(f => f.Id == folderId,
                        includeProperties: FolderDefaultProperties)
                    .First();

                return Observable.Return(new FolderImageContainer(folder, _fileSystem));
            });
        }

        private IObservable<IImageContainer> GetAlbumImageContainer(int albumId)
        {
            return Observable.Defer<IImageContainer>(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                var album = unitOfWork.AlbumRepository
                    .Get(a => a.Id == albumId,
                        includeProperties: AlbumDefaultProperties)
                    .First();

                return Observable.Return(new AlbumImageContainer(album));
            });
        }
    }
}