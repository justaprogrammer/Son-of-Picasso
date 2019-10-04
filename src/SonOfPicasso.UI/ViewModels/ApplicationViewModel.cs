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
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly Func<ImageContainerViewModel> _imageContainerViewModelFactory;
        private readonly IImageManagementService _imageManagementService;
        private readonly Func<ImageViewModel> _imageViewModelFactory;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public ApplicationViewModel(ILogger logger,
            ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<ImageViewModel> imageViewModelFactory,
            Func<ImageContainerViewModel> imageContainerViewModelFactory,
            ViewModelActivator activator)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageViewModelFactory = imageViewModelFactory;
            _imageContainerViewModelFactory = imageContainerViewModelFactory;
            Activator = activator;

            var images = new ObservableCollectionExtended<ImageViewModel>();
            Images = images;

            var imageContainers = new ObservableCollectionExtended<ImageContainerViewModel>();
            ImageContainers = imageContainers;
            
            var imageRefList = new ObservableCollectionExtended<ImageRef>();
            ImageRefs = imageRefList;

            AddFolder = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddFolder);
            AddFolderInteraction = new Interaction<Unit, string>();

            NewAlbum = ReactiveCommand.CreateFromObservable(ExecuteNewAlbum);
            NewAlbumInteraction = new Interaction<Unit, AddAlbumViewModel>();

            ImageCache = new SourceCache<Image, int>(model => model.Id);
            ImageContainerCache = new SourceCache<ImageContainer, string>(imageContainer => imageContainer.Id);

            this.WhenActivated(d =>
            {
                d(ImageContainerCache
                    .Connect()
                    .Transform(CreateImageContainerViewModel)
                    .DisposeMany()
                    .Sort(SortExpressionComparer<ImageContainerViewModel>
                        .Ascending(model => model.ContainerType == ImageContainerTypeEnum.Folder)
                        .ThenByDescending(model => model.Date))
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(imageContainers)
                    .Subscribe());

                d(ImageContainerCache
                    .Connect()
                    .TransformMany(container => container.ImageRefs, imageRef => imageRef.Id)
                    .Sort(SortExpressionComparer<ImageRef>
                        .Ascending(model => model.ContainerType == ImageContainerTypeEnum.Folder)
                        .ThenByDescending(model => model.ContainerDate)
                        .ThenBy(model => model.Date))
                    .Bind(imageRefList)
                    .Subscribe());

                d(ImageContainerCache
                    .PopulateFrom(_imageManagementService.GetAllImageContainers()));
            });
        }

        public Interaction<Unit, string> AddFolderInteraction { get; set; }

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; set; }

        private SourceCache<ImageContainer, string> ImageContainerCache { get; }

        private SourceCache<Image, int> ImageCache { get; }

        public ObservableCollection<ImageViewModel> Images { get; }

        public ObservableCollectionExtended<ImageContainerViewModel> ImageContainers { get; }
        public ObservableCollectionExtended<ImageRef> ImageRefs { get; }

        public ReactiveCommand<Unit, Unit> AddFolder { get; }

        public ReactiveCommand<Unit, Unit> NewAlbum { get; }

        public ViewModelActivator Activator { get; }

        private ImageContainerViewModel CreateImageContainerViewModel(ImageContainer imageContainer)
        {
            var imageContainerViewModel = _imageContainerViewModelFactory();
            imageContainerViewModel.Initialize(imageContainer);
            return imageContainerViewModel;
        }

        private ImageViewModel CreateImageViewModel(Image image)
        {
            var imageViewModel = _imageViewModelFactory();
            imageViewModel.Initialize(image);
            return imageViewModel;
        }

        private IObservable<Unit> ExecuteNewAlbum()
        {
            return NewAlbumInteraction.Handle(Unit.Default)
                .ObserveOn(_schedulerProvider.TaskPool)
                .Select(model =>
                {
                    if (model == null)
                        return Observable.Return(Unit.Default);

                    return _imageManagementService.CreateAlbum(model)
                        .Select(album =>
                        {
                            // ImageContainerCache.AddOrUpdate(CreateAlbumViewModel(album));
                            return Unit.Default;
                        });
                })
                .SelectMany(observable => observable);
        }

        private IObservable<Unit> ExecuteAddFolder(Unit unit)
        {
            return AddFolderInteraction.Handle(Unit.Default)
                .ObserveOn(_schedulerProvider.TaskPool)
                .Select(s =>
                {
                    if (s == null) return Observable.Return(Unit.Default);

                    return _imageManagementService.ScanFolder(s)
                        .ToArray()
                        .Select(images => Unit.Default);
                })
                .SelectMany(observable => observable);
        }
    }
}