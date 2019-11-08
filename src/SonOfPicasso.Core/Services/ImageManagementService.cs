using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
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
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly Func<IUnitOfWork> _unitOfWorkFactory;

        public ImageManagementService(IFileSystem fileSystem,
            IImageLocationService imageLocationService,
            Func<IUnitOfWork> unitOfWorkFactory,
            ISchedulerProvider schedulerProvider,
            IExifDataService exifDataService)
        {
            _fileSystem = fileSystem;
            _imageLocationService = imageLocationService;
            _unitOfWorkFactory = unitOfWorkFactory;
            _schedulerProvider = schedulerProvider;
            _exifDataService = exifDataService;
        }

        public IObservable<IImageContainer> ScanFolder(string path)
        {
            return Observable.Using(_unitOfWorkFactory, unitOfWork =>
                {
                    var imagesAtPath = unitOfWork.ImageRepository
                        .Get(image => image.Path.StartsWith(path))
                        .Select(image => image.Path)
                        .ToHashSet();

                    return _imageLocationService
                        .GetImages(path)
                        .Where(imagePath => !imagesAtPath.Contains(imagePath))
                        .GroupBy(s => _fileSystem.Path.GetDirectoryName(s))
                        .SelectMany(async groupedObservable =>
                        {
                            var folderPath = groupedObservable.Key;
                            var images = await groupedObservable
                                .SelectMany(imagePath => _exifDataService
                                    .GetExifData(imagePath)
                                    .Select(exifData => new Image
                                    {
                                        Path = imagePath,
                                        ExifData = exifData
                                    }))
                                .ToArray();

                            var folder = unitOfWork.FolderRepository
                                .Get(d => d.Path == folderPath, includeProperties: FolderDefaultProperties)
                                .FirstOrDefault();

                            if (folder != null)
                            {
                                folder.Images.AddRange(images);
                            }
                            else
                            {
                                var minDate = images.Select(image => image.ExifData.DateTime).Min();

                                folder = new Folder
                                {
                                    Path = folderPath,
                                    Images = images.ToList(),
                                    Date = minDate.Date
                                };

                                unitOfWork.FolderRepository.Insert(folder);
                            }

                            unitOfWork.Save();

                            return (IImageContainer) new FolderImageContainer(folder, _fileSystem);
                        });
                })
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> CreateAlbum(ICreateAlbum createAlbum)
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();

                    var album = new Album
                    {
                        Name = createAlbum.AlbumName,
                        Date = createAlbum.AlbumDate,
                        AlbumImages = new List<AlbumImage>()
                    };

                    unitOfWork.AlbumRepository.Insert(album);
                    unitOfWork.Save();

                    return Observable.Return(album);
                })
                .Select(album => (IImageContainer) new AlbumImageContainer(album))
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
            return Observable.Create<IObservable<IImageContainer>>(observer =>
                {
                    var unitOfWork = _unitOfWorkFactory();

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

                    observer.OnNext(GetAlbumImageContainer(albumId));
                    observer.OnCompleted();

                    return unitOfWork;
                })
                .SelectMany(observable => observable)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> AddImage(string path)
        {
            return Observable.DeferAsync(async token =>
                {
                    using var unitOfWork = _unitOfWorkFactory();
                    var image = unitOfWork.ImageRepository
                        .Get(i => i.Path.Equals(path), includeProperties: "Folder")
                        .FirstOrDefault();

                    if (image != null)
                        return Observable.Empty<int>();

                    var exifData = await _exifDataService.GetExifData(path);

                    image = new Image
                    {
                        Path = path,
                        ExifData = exifData
                    };

                    var directory = _fileSystem.Path.GetDirectoryName(path);
                    var folder = unitOfWork.FolderRepository
                        .Get(f => f.Path.Equals(directory))
                        .FirstOrDefault();

                    if (folder != null)
                    {
                        folder.Images.Add(image);
                    }
                    else
                    {
                        folder = new Folder
                        {
                            Path = path,
                            Date = exifData.DateTime.Date,
                            Images = new List<Image> {image}
                        };

                        unitOfWork.FolderRepository.Insert(folder);
                    }

                    unitOfWork.Save();

                    return Observable.Return(folder.Id);
                })
                .SelectMany(GetFolderImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> DeleteImage(string path)
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();
                    var image = unitOfWork.ImageRepository
                        .Get(i => i.Path.Equals(path))
                        .FirstOrDefault();

                    if (image == null)
                        return Observable.Empty<int>();

                    var imageFolderId = image.FolderId;
                    unitOfWork.ImageRepository.Delete(image);
                    unitOfWork.Save();

                    return Observable.Return(imageFolderId);
                })
                .SelectMany(GetFolderImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> UpdateImage(string path)
        {
            return Observable.DeferAsync(async token =>
                {
                    using var unitOfWork = _unitOfWorkFactory();
                    var image = unitOfWork.ImageRepository
                        .Get(i => i.Path.Equals(path))
                        .First();

                    var exifData = await _exifDataService.GetExifData(path);
                    exifData.Id = image.ExifDataId;

                    unitOfWork.ExifDataRepository.Update(exifData);

                    unitOfWork.Save();

                    return Observable.Return(image.FolderId);
                })
                .SelectMany(GetFolderImageContainer)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<IImageContainer> RenameImage(string oldPath, string newPath)
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();

                    var image = unitOfWork.ImageRepository
                        .Get(i => i.Path.Equals(oldPath), includeProperties: "ExifData")
                        .First();

                    image.Path = newPath;

                    var oldFolderId = image.FolderId;

                    var oldDirectoryPath = _fileSystem.Path.GetDirectoryName(oldPath);
                    var newDirectoryPath = _fileSystem.Path.GetDirectoryName(newPath);

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
                            Images = new List<Image> {image}
                        };

                        unitOfWork.FolderRepository.Insert(folder);
                    }
                    else
                    {
                        image.FolderId = folder.Id;
                    }

                    return new[] {oldFolderId, folder.Id}.ToObservable();
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