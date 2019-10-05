using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ReactiveObject, IActivatableViewModel, IApplicationViewModel
    {
        private readonly Func<ImageContainerViewModel> _imageContainerViewModelFactory;
        private readonly IImageManagementService _imageManagementService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private ImageViewModel _selectedItem;

        public ApplicationViewModel(ILogger logger,
            ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<ImageContainerViewModel> imageContainerViewModelFactory,
            ViewModelActivator activator)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageContainerViewModelFactory = imageContainerViewModelFactory;
            Activator = activator;

            var imageContainerViewModels = new ObservableCollectionExtended<ImageContainerViewModel>();
            ImageContainerViewModels = imageContainerViewModels;

            AddFolder = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddFolder);
            AddFolderInteraction = new Interaction<Unit, string>();

            NewAlbum = ReactiveCommand.CreateFromObservable(ExecuteNewAlbum);
            NewAlbumInteraction = new Interaction<Unit, AddAlbumViewModel>();

            ImageContainerCache = new SourceCache<ImageContainer, string>(imageContainer => imageContainer.Id);

            this.WhenActivated(d =>
            {
                ImageContainerCache
                    .Connect()
                    .Transform(CreateImageContainerViewModel)
                    .DisposeMany()
                    .Sort(SortExpressionComparer<ImageContainerViewModel>
                        .Ascending(model => model.ContainerType == ImageContainerTypeEnum.Folder)
                        .ThenByDescending(model => model.Date))
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(imageContainerViewModels)
                    .Subscribe()
                    .DisposeWith(d);

                ImageContainerCache
                    .PopulateFrom(_imageManagementService.GetAllImageContainers())
                    .DisposeWith(d);

                this.WhenAnyValue(model => model.SelectedItem)
                    .Subscribe(model =>
                    {
                        ;
                    })
                    .DisposeWith(d);
            });
        }

        public Interaction<Unit, string> AddFolderInteraction { get; set; }

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; set; }

        private SourceCache<ImageContainer, string> ImageContainerCache { get; }

        public IObservableCollection<ImageContainerViewModel> ImageContainerViewModels { get; }

        public ReactiveCommand<Unit, Unit> AddFolder { get; }

        public ReactiveCommand<Unit, Unit> NewAlbum { get; }

        public ImageViewModel SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public ViewModelActivator Activator { get; }

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