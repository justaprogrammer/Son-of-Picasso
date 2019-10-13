using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ViewModelBase, IDisposable
    {
        private readonly Func<ImageContainerViewModel> _imageContainerViewModelFactory;
        private readonly IImageManagementService _imageManagementService;
        private readonly Func<ImageViewModel> _imageViewModelFactory;
        private readonly ISchedulerProvider _schedulerProvider;

        public ApplicationViewModel(ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<ImageContainerViewModel> imageContainerViewModelFactory,
            Func<ImageViewModel> imageViewModelFactory,
            ViewModelActivator activator) : base(activator)
        {
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageContainerViewModelFactory = imageContainerViewModelFactory;
            _imageViewModelFactory = imageViewModelFactory;

            var imageContainerViewModels = new ObservableCollectionExtended<ImageContainerViewModel>();
            ImageContainerViewModels = imageContainerViewModels;

            var imageViewModels = new ObservableCollectionExtended<ImageViewModel>();
            ImageViewModels = imageViewModels;

            AddFolder = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddFolder);
            AddFolderInteraction = new Interaction<Unit, string>();

            NewAlbum = ReactiveCommand.CreateFromObservable(ExecuteNewAlbum);
            NewAlbumInteraction = new Interaction<Unit, AddAlbumViewModel>();

            ImageContainerCache = new SourceCache<ImageContainer, string>(imageContainer => imageContainer.Id);

            this.WhenActivated(d =>
            {
                var imageContainerViewModelCache = ImageContainerCache
                    .Connect()
                    .Transform(CreateImageContainerViewModel)
                    .DisposeMany()
                    .AsObservableCache()
                    .DisposeWith(d);

                imageContainerViewModelCache
                    .Connect()
                    .Sort(SortExpressionComparer<ImageContainerViewModel>
                        .Ascending(model => model.ContainerType == ImageContainerTypeEnum.Folder)
                        .ThenByDescending(model => model.Date))
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(imageContainerViewModels)
                    .Subscribe()
                    .DisposeWith(d);

                imageContainerViewModelCache
                    .Connect()
                    .TransformMany(
                        containerViewModel => containerViewModel.ImageRefs.Select(imageRef => CreateImageViewModel(imageRef, containerViewModel)),
                        imageViewModel => imageViewModel.ImageRefId)
                    .DisposeMany()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(imageViewModels)
                    .Subscribe()
                    .DisposeWith(d);

                ImageContainerCache
                    .PopulateFrom(_imageManagementService.GetAllImageContainers().ToArray())
                    .DisposeWith(d);
            });
        }

        public Interaction<Unit, string> AddFolderInteraction { get; set; }

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; set; }

        private SourceCache<ImageContainer, string> ImageContainerCache { get; }

        public ObservableCollection<ImageContainerViewModel> ImageContainerViewModels { get; }

        public ObservableCollectionExtended<ImageViewModel> ImageViewModels { get; }

        public ReactiveCommand<Unit, Unit> AddFolder { get; }

        public ReactiveCommand<Unit, Unit> NewAlbum { get; }

        public void Dispose()
        {
            ImageContainerCache?.Dispose();
        }

        private ImageViewModel CreateImageViewModel(ImageRef imageRef, ImageContainerViewModel imageContainerViewModel)
        {
            var imageViewModel = _imageViewModelFactory();
            imageViewModel.Initialize(imageRef, imageContainerViewModel);
            return imageViewModel;
        }

        private ImageContainerViewModel CreateImageContainerViewModel(ImageContainer imageContainer)
        {
            var imageContainerViewModel = _imageContainerViewModelFactory();
            imageContainerViewModel.Initialize(imageContainer, this);
            return imageContainerViewModel;
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
                        .Select(imageContainer =>
                        {
                            ImageContainerCache.AddOrUpdate(imageContainer);
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
                    if (s != null)
                    {
                        ImageContainerCache.PopulateFrom(_imageManagementService.ScanFolder(s).ToArray());
                    }

                    return Observable.Return(Unit.Default);
                })
                .SelectMany(observable => observable);
        }
    }
}