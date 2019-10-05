using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ViewModelBase
    {
        private readonly Func<ImageContainerViewModel> _imageContainerViewModelFactory;
        private readonly IImageManagementService _imageManagementService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ObservableAsPropertyHelper<ImageViewModel> _selectedImage;
        private readonly ObservableAsPropertyHelper<ImageContainerViewModel> _selectedImageContainer;
        private readonly ReplaySubject<ImageContainerViewModel> _selectedImageContainerReplay;
        private readonly ReplaySubject<ImageViewModel> _selectedImageReplay;
        private readonly ObservableAsPropertyHelper<ImageRowViewModel> _selectedImageRow;
        private readonly ReplaySubject<ImageRowViewModel> _selectedImageRowReplay;

        public ApplicationViewModel(ILogger logger,
            ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<ImageContainerViewModel> imageContainerViewModelFactory,
            ViewModelActivator activator) : base(activator)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageContainerViewModelFactory = imageContainerViewModelFactory;

            var imageContainerViewModels = new ObservableCollectionExtended<ImageContainerViewModel>();
            ImageContainerViewModels = imageContainerViewModels;

            AddFolder = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddFolder);
            AddFolderInteraction = new Interaction<Unit, string>();

            NewAlbum = ReactiveCommand.CreateFromObservable(ExecuteNewAlbum);
            NewAlbumInteraction = new Interaction<Unit, AddAlbumViewModel>();

            ImageContainerCache = new SourceCache<ImageContainer, string>(imageContainer => imageContainer.Id);

            _selectedImageContainerReplay = new ReplaySubject<ImageContainerViewModel>();
            _selectedImageContainer = _selectedImageContainerReplay.ToProperty(this, nameof(SelectedImageContainer));

            _selectedImageRowReplay = new ReplaySubject<ImageRowViewModel>(1);
            _selectedImageRow = _selectedImageRowReplay.ToProperty(this, nameof(SelectedImageRow));

            _selectedImageReplay = new ReplaySubject<ImageViewModel>();
            _selectedImage = _selectedImageReplay.ToProperty(this, nameof(SelectedImage));

            this.WhenActivated(d =>
            {
                var imageContainerViewModelCache = ImageContainerCache
                    .Connect()
                    .Transform(CreateImageContainerViewModel)
                    .DisposeMany()
                    .AsObservableCache()
                    .DisposeWith(d);

                imageContainerViewModelCache.Connect()
                    .WhenAnyPropertyChanged(nameof(ImageContainerViewModel.SelectedImageRow),
                        nameof(ImageContainerViewModel.SelectedImage))
                    .Subscribe(imageContainerViewModel =>
                    {
                        var selectedImageRowChanged = imageContainerViewModel.SelectedImageRow != null
                                                      && imageContainerViewModel.SelectedImageRow != SelectedImageRow;

                        var selectedImageChanged = imageContainerViewModel.SelectedImage != null
                                                   && imageContainerViewModel.SelectedImage != SelectedImage;

                        var selectedContainerClearing = imageContainerViewModel.SelectedImageRow == null
                                                        && imageContainerViewModel == SelectedImageContainer;

                        if (selectedImageRowChanged
                            || selectedContainerClearing)
                        {
                            if (imageContainerViewModel.SelectedImageRow == null)
                            {
                                _selectedImageContainerReplay.OnNext(null);
                                _selectedImageRowReplay.OnNext(null);
                                _selectedImageReplay.OnNext(null);
                            }
                            else
                            {
                                _selectedImageContainerReplay.OnNext(imageContainerViewModel);
                                _selectedImageRowReplay.OnNext(imageContainerViewModel.SelectedImageRow);
                                _selectedImageReplay.OnNext(imageContainerViewModel.SelectedImageRow.SelectedImage);
                            }
                        }

                        if (selectedImageChanged) _selectedImageReplay.OnNext(SelectedImageContainer.SelectedImage);
                    })
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

                ImageContainerCache
                    .PopulateFrom(_imageManagementService.GetAllImageContainers())
                    .DisposeWith(d);
            });
        }

        public Interaction<Unit, string> AddFolderInteraction { get; set; }

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; set; }

        private SourceCache<ImageContainer, string> ImageContainerCache { get; }

        public IObservableCollection<ImageContainerViewModel> ImageContainerViewModels { get; }

        public ReactiveCommand<Unit, Unit> AddFolder { get; }

        public ReactiveCommand<Unit, Unit> NewAlbum { get; }

        public ImageContainerViewModel SelectedImageContainer => _selectedImageContainer.Value;

        public ImageRowViewModel SelectedImageRow => _selectedImageRow.Value;

        public ImageViewModel SelectedImage => _selectedImage.Value;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _selectedImage?.Dispose();
                _selectedImageContainer?.Dispose();
                _selectedImageContainerReplay?.Dispose();
                _selectedImageReplay?.Dispose();
                _selectedImageRow?.Dispose();
                _selectedImageRowReplay?.Dispose();
                ImageContainerCache?.Dispose();
                AddFolder?.Dispose();
                NewAlbum?.Dispose();
            }

            base.Dispose(disposing);
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