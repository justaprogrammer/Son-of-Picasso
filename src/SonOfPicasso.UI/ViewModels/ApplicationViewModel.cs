using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly Func<ImageFolderViewModel> _imageFolderViewModelFactory;
        private readonly Func<AlbumViewModel> _albumViewModelFactory;
        private readonly IImageManagementService _imageManagementService;
        private readonly Func<ImageViewModel> _imageViewModelFactory;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ApplicationViewModel(ILogger logger,
            ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<ImageViewModel> imageViewModelFactory,
            Func<ImageFolderViewModel> imageFolderViewModelFactory,
            Func<AlbumViewModel> albumViewModelFactory,
            ViewModelActivator activator)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageViewModelFactory = imageViewModelFactory;
            _imageFolderViewModelFactory = imageFolderViewModelFactory;
            _albumViewModelFactory = albumViewModelFactory;
            Activator = activator;

            var images = new ObservableCollectionExtended<ImageViewModel>();
            Images = images;

            var imageContainers = new ObservableCollectionExtended<IImageContainerViewModel>();
            ImageContainers = imageContainers;

            AddFolder = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddFolder);
            AddFolderInteraction = new Interaction<Unit, string>();

            NewAlbum = ReactiveCommand.CreateFromObservable(ExecuteNewAlbum);
            NewAlbumInteraction = new Interaction<Unit, AddAlbumViewModel>();

            ImageCache = new SourceCache<Image, int>(model => model.Id);
            ImageContainerCache = new SourceCache<(Folder folder, Album album), string>(tuple =>
            {
                var (folder, album) = tuple;
                if (folder != null) return ImageFolderViewModel.GetContainerId(folder);
                if (album != null) return AlbumViewModel.GetContainerId(album);

                throw new InvalidOperationException("Folder and Album are both null");
            });

            this.WhenActivated(d =>
            {
                d(ImageContainerCache
                    .Connect()
                    .Transform(tuple =>
                    {
                        var (folder, album) = tuple;
                        if (folder != null) return (IImageContainerViewModel) CreateImageFolderViewModel(folder);
                        if (album != null) return CreateAlbumViewModel(album) as IImageContainerViewModel;

                        throw new InvalidOperationException("Folder and Album are both null");
                    })
                    .DisposeMany()
                    .Sort(SortExpressionComparer<IImageContainerViewModel>
                        .Ascending(model => model.ContainerType == ContainerTypeEnum.Folder)
                        .ThenByDescending(model => model.Date))
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(imageContainers)
                    .Subscribe());

//                d(ImageContainerCache
//                    .Connect()
//                    .Sort(SortExpressionComparer<IImageContainerViewModel>
//                        .Ascending(model => model.ContainerType == ContainerTypeEnum.Folder)
//                        .ThenByDescending(model => model.Date))
//                    .TransformMany(model => model.ImageIds, i => i)
//                    .Transform(i => ImageCache.Lookup(i).Value)
//                    .Transform(CreateImageViewModel)
//                    .Sort(SortExpressionComparer<ImageViewModel>.Descending(model => model.ExifData.DateTime))
//                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
//                    .Bind(images)
//                    .Subscribe());

                var allImages = _imageManagementService.GetImagesWithDirectoryAndExif()
                    .Publish();

                d(ImageCache
                    .PopulateFrom(allImages));

                d(ImageContainerCache
                    .PopulateFrom(allImages
                        .Distinct(directory => directory.Path)
                        .Select(image => (image.Folder, (Album)null))
                        .ToArray()));

                d(allImages.Connect());

               d(ImageContainerCache
                   .PopulateFrom(_imageManagementService
                       .GetAllAlbumsWithAlbumImages()
                       .Select(album => ((Folder)null, album))));
            });
        }

        public Interaction<Unit, string> AddFolderInteraction { get; set; }

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; set; }

        private SourceCache<(Folder folder, Album album), string> ImageContainerCache { get; }

        private SourceCache<Image, int> ImageCache { get; }

        public ObservableCollection<ImageViewModel> Images { get; }

        public ObservableCollectionExtended<IImageContainerViewModel> ImageContainers { get; }

        public ReactiveCommand<Unit, Unit> AddFolder { get; }

        public ReactiveCommand<Unit, Unit> NewAlbum { get; }

        public ViewModelActivator Activator { get; }

        private AlbumViewModel CreateAlbumViewModel(Album album)
        {
            var albumViewModel = _albumViewModelFactory();
            albumViewModel.Initialize(album);
            return albumViewModel;
        }

        private ImageFolderViewModel CreateImageFolderViewModel(Folder folder)
        {
            var imageFolderViewModel = _imageFolderViewModelFactory();
            imageFolderViewModel.Initialize(folder);
            return imageFolderViewModel;
        }

        private ImageViewModel CreateImageViewModel(Image image)
        {
            var imageViewModel = _imageViewModelFactory();
            imageViewModel.Initialize(image);
            return imageViewModel;
        }

        private IObservable<Unit> ExecuteNewAlbum()
        {
            throw new NotImplementedException();

//            return NewAlbumInteraction.Handle(Unit.Default)
//                .ObserveOn(_schedulerProvider.TaskPool)
//                .Select(model =>
//                {
//                    if (model == null)
//                        return Observable.Return(Unit.Default);
//
//                    return _imageManagementService.CreateAlbum(model)
//                        .Select(album =>
//                        {
//                            ImageContainerCache.AddOrUpdate(CreateAlbumViewModel(album));
//                            return Unit.Default;
//                        });
//                })
//                .SelectMany(observable => observable);
        }

        private IObservable<Unit> ExecuteAddFolder(Unit unit)
        {
            throw new NotImplementedException();
       
//            return AddFolderInteraction.Handle(Unit.Default)
//                .ObserveOn(_schedulerProvider.TaskPool)
//                .Select(s =>
//                {
//                    if (s == null) return Observable.Return(Unit.Default);
//
//                    var discoveredImages = _imageManagementService.ScanFolder(s)
//                        .Publish();
//
//                    var addImages = discoveredImages
//                        .ToArray()
//                        .Select(images =>
//                        {
//                            ImageCache.AddOrUpdate(images.Select(CreateImageViewModel));
//                            return Unit.Default;
//                        });
//
//                    var discoveredFolders = discoveredImages
//                        .Select(image => image.Folder)
//                        .GroupBy(directory => directory.Path)
//                        .Select(groupedObservable => groupedObservable.FirstAsync())
//                        .SelectMany(observable1 => observable1)
//                        .Select(directory => (directory,
//                            ImageContainerCache.Lookup(ImageFolderViewModel.GetContainerId(directory))));
//
//                    var addFolders = discoveredFolders
//                        .Where(tuple => !tuple.Item2.HasValue)
//                        .Select(tuple => tuple.directory)
//                        .ToArray()
//                        .Select(directories =>
//                        {
//                            ImageContainerCache.AddOrUpdate(directories.Select(CreateImageFolderViewModel));
//                            return Unit.Default;
//                        });
//
//                    var updateFolders = discoveredFolders
//                        .Where(tuple => tuple.Item2.HasValue)
//                        .ToArray()
//                        .Select(tuples =>
//                        {
//                            foreach (var tuple in tuples)
//                            {
//                                var imageContainerViewModel = tuple.Item2.Value;
//                                if (imageContainerViewModel is ImageFolderViewModel imageFolderViewModel)
//                                    imageFolderViewModel.Initialize(tuple.directory);
//                            }
//
//                            return Unit.Default;
//                        });
//
//                    discoveredImages.Connect();
//
//                    return addImages.Zip(addFolders, updateFolders, (unit, _, __) => unit);
//                })
//                .SelectMany(observable => observable);
        }
    }
}