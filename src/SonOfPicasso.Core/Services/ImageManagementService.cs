using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
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

        public IObservable<ImageContainer> ScanFolder(string path)
        {
            return Observable.Create<Folder>(async observer =>
                {
                    using (var unitOfWork = _unitOfWorkFactory())
                    {
                        await _imageLocationService.GetImages(path)
                            .Where(imagePath => !unitOfWork.ImageRepository.Get(image => image.Path == imagePath).Any())
                            .SelectMany(imagePath => _exifDataService.GetExifData(imagePath).Select(exifData =>
                                (_fileSystem.Path.GetDirectoryName(imagePath),
                                    new Image {Path = imagePath, ExifData = exifData})))
                            .GroupBy(tuple => tuple.Item1, tuple => tuple.Item2)
                            .SelectMany(observable => observable.ToArray().Select(images => (observable.Key, images)))
                            .Select(tuple =>
                            {
                                var folder = unitOfWork.FolderRepository
                                    .Get(d => d.Path == tuple.Key, includeProperties: "Images,Images.ExifData")
                                    .FirstOrDefault();

                                if (folder == null)
                                {
                                    var minDate = tuple.images.Select((image, i) => image.ExifData.DateTime).Min();

                                    folder = new Folder
                                    {
                                        Path = tuple.Key,
                                        Images = new List<Image>(), 
                                        Date = minDate.Date
                                    };

                                    unitOfWork.FolderRepository.Insert(folder);
                                }

                                folder.Images ??= new List<Image>();
                                folder.Images.AddRange(tuple.images);

                                unitOfWork.Save();

                                observer.OnNext(folder);

                                return Unit.Default;
                            }).LastOrDefaultAsync();
                    }

                    observer.OnCompleted();
                    return Disposable.Empty;
                })
                .Select(folder => (ImageContainer) new FolderImageContainer(folder))
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
                    .Get(includeProperties: "Images,Images.ExifData")
                    .ToArray();

                foreach (var folder in folders) observer.OnNext(new FolderImageContainer(folder));

                observer.OnCompleted();

                return unitOfWork;
            });

            var selectAlbums = Observable.Create<ImageContainer>(observer =>
            {
                var unitOfWork = _unitOfWorkFactory();

                var albums = unitOfWork.AlbumRepository
                    .Get(includeProperties: "AlbumImages,AlbumImages.Image")
                    .ToArray();

                foreach (var album in albums) observer.OnNext(new AlbumImageContainer(album));

                observer.OnCompleted();

                return unitOfWork;
            });

            return selectFolders.Merge(selectAlbums)
                .SubscribeOn(_schedulerProvider.TaskPool);
        }
    }
}