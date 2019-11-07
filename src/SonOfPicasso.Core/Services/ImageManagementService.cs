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
                            .Get(d => d.Path == folderPath, includeProperties: "Images,Images.ExifData")
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
            });
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
                    .Get(includeProperties: "Images,Images.ExifData")
                    .ToArray();

                foreach (var folder in folders) observer.OnNext(new FolderImageContainer(folder, _fileSystem));

                observer.OnCompleted();

                return unitOfWork;
            });

            var selectAlbums = Observable.Create<IImageContainer>(observer =>
            {
                var unitOfWork = _unitOfWorkFactory();

                var albums = unitOfWork.AlbumRepository
                    .Get(includeProperties: "AlbumImages,AlbumImages.Image,AlbumImages.Image.ExifData")
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
                .SelectMany(observable => observable);
        }

        public IObservable<IImageContainer> GetAlbumImageContainer(int albumId)
        {
            return Observable.Create<IImageContainer>(observer =>
            {
                var unitOfWork = _unitOfWorkFactory();

                var album = unitOfWork.AlbumRepository
                    .Get(album => album.Id == albumId,
                        includeProperties: "AlbumImages,AlbumImages.Image,AlbumImages.Image.ExifData")
                    .First();

                observer.OnNext(new AlbumImageContainer(album));
                observer.OnCompleted();

                return unitOfWork;
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }
    }
}