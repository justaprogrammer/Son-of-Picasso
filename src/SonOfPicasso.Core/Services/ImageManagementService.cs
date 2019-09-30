using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Serilog;
using SonOfPicasso.Core.Interfaces;
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

        public IObservable<Folder> GetAllDirectoriesWithImages()
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();

                    var directories = unitOfWork.FolderRepository.Get(includeProperties: "Images")
                        .ToArray();

                    return Observable.Return(directories);
                })
                .SelectMany(directories => directories)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Album> GetAllAlbumsWithAlbumImages()
        {
            return Observable.Defer(() =>
                {
                    using var unitOfWork = _unitOfWorkFactory();

                    var directories = unitOfWork.AlbumRepository.Get(includeProperties: "AlbumImages")
                        .ToArray();

                    return Observable.Return(directories);
                })
                .SelectMany(directories => directories)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Image> ScanFolder(string path)
        {
            return Observable.DeferAsync(async task =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                if (!_fileSystem.Directory.Exists(path))
                    throw new SonOfPicassoException($"Path: `{path}` does not exist");

                var foundImagePaths = await _imageLocationService.GetImages(path)
                    .ToArray();


                var images = await foundImagePaths
                    .Where(s => !unitOfWork.ImageRepository.Get(image => image.Path == s).Any())
                    .ToObservable()
                    .SelectMany(imagePath =>
                        _exifDataService.GetExifData(imagePath).Select(exifData => (imagePath, exifData)))
                    .Select(tuple =>
                    {
                        return new Image
                        {
                            Path = tuple.imagePath,
                            ExifData = tuple.exifData
                        };
                    })
                    .ToArray();

                var groups = images.ToLookup(image => _fileSystem.FileInfo.FromFileName(image.Path).DirectoryName);

                foreach (var group in groups)
                {
                    var minDate = group.Select((image, i) => image.ExifData.DateTime).Min();

                    var folder = unitOfWork.FolderRepository
                        .Get(d => d.Path == group.Key)
                        .FirstOrDefault();

                    if (folder == null)
                    {
                        folder = new Folder { Path = group.Key, Images = new List<Image>(group), Date = minDate.Date };

                        unitOfWork.FolderRepository.Insert(folder);
                    }
                    else
                    {
                        folder.Images ??= new List<Image>();
                        folder.Images.AddRange(group);
                    }
                }

                unitOfWork.Save();

                return Observable.Return(images);
            })
            .SelectMany(images => images)
            .SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<Album> CreateAlbum(string name)
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                var album = new Album { Name = name };

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

        public IObservable<Image[]> GetImagesWithDirectoryAndExif()
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                var images = unitOfWork.ImageRepository
                    .Get(includeProperties: "Folder,ExifData")
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

                    var images = imageIds.Select(imageId =>
                    {
                        var image = unitOfWork.ImageRepository.GetById(imageId);

                        unitOfWork.AlbumImageRepository.Insert(new AlbumImage { Album = album, Image = image });

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