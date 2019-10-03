using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
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
                        .Select(tuple => new Image
                        {
                            Path = tuple.imagePath,
                            ExifData = tuple.exifData
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
                            folder = new Folder
                                {Path = group.Key, Images = new List<Image>(group), Date = minDate.Date};

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

        public IObservable<Album> CreateAlbum(ICreateAlbum createAlbum)
        {
            return Observable.Defer(() =>
            {
                using var unitOfWork = _unitOfWorkFactory();

                var album = new Album {Name = createAlbum.AlbumName, Date = createAlbum.AlbumDate};

                unitOfWork.AlbumRepository.Insert(album);
                unitOfWork.Save();

                return Observable.Return(album);
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        public IObservable<ImageContainer> GetAllImageContainers()
        {
            var selectFolders = Observable.Create<ImageContainer>(observer =>
            {
                var unitOfWork = _unitOfWorkFactory();

                var folders = unitOfWork.FolderRepository
                    .Get()
                    .ToArray();

                foreach (var folder in folders)
                {
                    observer.OnNext(new FolderImageContainer(folder));
                }

                observer.OnCompleted();

                return unitOfWork;
            });

            var selectAlbums = Observable.Create<ImageContainer>(observer =>
            {
                var unitOfWork = _unitOfWorkFactory();

                var albums = unitOfWork.AlbumRepository
                    .Get()
                    .ToArray();

                foreach (var album in albums)
                {
                    observer.OnNext(new AlbumImageContainer(album));
                }

                observer.OnCompleted();

                return unitOfWork;
            });

            return selectFolders.Merge(selectAlbums);
        }
    }
}