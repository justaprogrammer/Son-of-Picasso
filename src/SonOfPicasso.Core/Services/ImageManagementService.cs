using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.Data.Repository;

namespace SonOfPicasso.Core.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly IExifDataService _exifDataService;
        private readonly IFileSystem _fileSystem;
        private readonly IImageLocationService _imageLocationService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly Func<IUnitOfWork> _unitOfWorkFactory;

        public ImageManagementService(ILogger logger,
            IFileSystem fileSystem,
            IImageLocationService imageLocationService,
            Func<IUnitOfWork> unitOfWorkFactory,
            ISchedulerProvider schedulerProvider,
            IExifDataService exifDataService)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _imageLocationService = imageLocationService;
            _unitOfWorkFactory = unitOfWorkFactory;
            _schedulerProvider = schedulerProvider;
            _exifDataService = exifDataService;
        }

        public IObservable<Directory> GetAllDirectoriesWithImages()
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();

                    var directories = unitOfWork.DirectoryRepository.Get(includeProperties: "Images")
                        .ToArray();

                    return Observable.Return(directories);
                })
                .SelectMany(directories => directories)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Image[]> ScanFolder(string path)
        {
            return Observable.DeferAsync(async task =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                if (!_fileSystem.Directory.Exists(path))
                    throw new SonOfPicassoException($"Path: `{path}` does not exist");

                var images = await _imageLocationService.GetImages(path)
                    .SelectMany(locatedImages => locatedImages)
                    .Where(s => !unitOfWork.ImageRepository.Get(image => image.Path == path).ToArray().Any())
                    .GroupBy(s => _fileSystem.FileInfo.FromFileName(s).DirectoryName)
                    .SelectMany(groupedObservable =>
                    {
                        var directory = unitOfWork.DirectoryRepository
                            .Get(d => d.Path == groupedObservable.Key)
                            .ToArray()
                            .FirstOrDefault();

                        if (directory == null)
                        {
                            directory = new Directory {Path = groupedObservable.Key, Images = new List<Image>()};
                            unitOfWork.DirectoryRepository.Insert(directory);
                        }

                        return groupedObservable
                            .Select(imagePath =>
                            {
                                var observable = _exifDataService
                                    .GetExifData(imagePath)
                                    .Select(exifData => { return (imagePath, exifData); });
                                return observable;
                            })
                            .SelectMany(observable => observable)
                            .Select(tuple =>
                            {
                                var image = new Image
                                {
                                    Path = tuple.imagePath,
                                    ExifData = tuple.exifData
                                };

                                directory.Images.Add(image);

                                return image;
                            });
                    }).ToArray();

                unitOfWork.Save();

                return Observable.Return(images);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Album> CreateAlbum(string name)
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                var album = new Album {Name = name};

                unitOfWork.AlbumRepository.Insert(album);
                unitOfWork.Save();

                return Observable.Return(album);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Album[]> GetAlbums()
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                var albums = unitOfWork.AlbumRepository.Get()
                    .ToArray();

                return Observable.Return(albums);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Image[]> GetImages()
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                var images = unitOfWork.ImageRepository.Get()
                    .ToArray();

                return Observable.Return(images);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Image> AddImagesToAlbum(int albumId, IList<int> imageIds)
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();

                    var album = unitOfWork.AlbumRepository.GetById(albumId);

                    var images = imageIds.Select(imageid =>
                    {
                        var image = unitOfWork.ImageRepository.GetById(imageid);

                        unitOfWork.AlbumImageRepository.Insert(new AlbumImage {Album = album, Image = image});

                        return image;
                    }).ToArray();

                    unitOfWork.Save();

                    return Observable.Return(images);
                })
                .SelectMany(observable => observable)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Unit> DeleteImages(IList<int> imageIds)
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                foreach (var imageId in imageIds) unitOfWork.ImageRepository.Delete(imageId);

                unitOfWork.Save();

                return Observable.Return(Unit.Default);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Unit> DeleteAlbums(IList<int> albumIds)
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                foreach (var albumId in albumIds) unitOfWork.AlbumRepository.Delete(albumId);

                unitOfWork.Save();

                return Observable.Return(Unit.Default);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Unit> RemoveImageFromAlbum(IList<int> albumImageIds)
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                foreach (var albumImageId in albumImageIds) unitOfWork.AlbumImageRepository.Delete(albumImageId);

                unitOfWork.Save();

                return Observable.Return(Unit.Default);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }
    }
}