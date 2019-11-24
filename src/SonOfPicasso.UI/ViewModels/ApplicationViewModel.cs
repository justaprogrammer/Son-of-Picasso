using System;
using System.Collections.Generic;
using System.Linq;
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
using SonOfPicasso.Core.Services;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ActivatableViewModelBase, IDisposable
    {
        private readonly IImageContainerManagementService _imageContainerManagementService;
        private readonly ILogger _logger;
        private readonly IObservableCache<ImageContainerViewModel, string> _imageContainerViewModelCache;

        private readonly IObservableCache<ImageViewModel, string> _imageViewModelCache;
        private readonly ISchedulerProvider _schedulerProvider;

        private readonly SourceCache<ImageViewModel, int> _selectedImagesSourceCache;
        private readonly IObservableCache<TrayImageViewModel, int> _trayImageCache;

        private readonly SourceCache<ImageViewModel, int> _trayImageSourceCache;

        private readonly Subject<IEnumerable<ImageViewModel>> _unselectImageSubject =
            new Subject<IEnumerable<ImageViewModel>>();

        private readonly Subject<IEnumerable<TrayImageViewModel>> _unselectTrayImageSubject =
            new Subject<IEnumerable<TrayImageViewModel>>();

        private ObservableAsPropertyHelper<int> _imagesViewportColumns;

        private double _imagesViewportWidth;

        private string _visibleItemContainerKey;

        public ApplicationViewModel(ISchedulerProvider schedulerProvider,
            IImageContainerManagementService imageContainerManagementService,
            IImageLoadingService imageLoadingService,
            ViewModelActivator activator,
            ILogger logger) : base(activator)
        {
            _schedulerProvider = schedulerProvider;
            _imageContainerManagementService = imageContainerManagementService;
            _logger = logger;

            _selectedImagesSourceCache = new SourceCache<ImageViewModel, int>(model => model.ImageId);

            FolderManager = 
                ReactiveCommand.CreateFromObservable<Unit, Unit>(
                    ExecuteFolderManager);
            
            AddFolder = 
                ReactiveCommand.CreateFromObservable<Unit, Unit>(
                    ExecuteAddFolder);
            
            NewAlbum = 
                ReactiveCommand.CreateFromObservable<Unit, ImageContainerViewModel>(
                    ExecuteNewAlbum);

            NewAlbumWithImages = ReactiveCommand
                .CreateFromObservable<IEnumerable<ImageViewModel>, ImageContainerViewModel>(
                    ExecuteNewAlbumWithImages);

            AddImagesToAlbum = ReactiveCommand
                    .CreateFromObservable<(IEnumerable<ImageViewModel>, ImageContainerViewModel),
                        ImageContainerViewModel>(ExecuteAddImagesToAlbum);

            var hasItemsInTray = this.WhenAnyValue(model => model.TrayImages.Count)
                .Select(propertyValue => propertyValue > 0);

            PinSelectedItems =
                ReactiveCommand.CreateFromObservable<IEnumerable<TrayImageViewModel>, Unit>(ExecutePinSelectedItems,
                    hasItemsInTray);
            ClearTrayItems =
                ReactiveCommand.CreateFromObservable<(IEnumerable<TrayImageViewModel>, bool), Unit>(
                    ExecuteClearTrayItems,
                    hasItemsInTray);
            AddTrayItemsToAlbum =
                ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddTrayItemsToAlbum, hasItemsInTray);

            _imageContainerViewModelCache = _imageContainerManagementService
                .ImageContainerCache
                .Connect()
                .Transform(imageContainer => new ImageContainerViewModel(imageContainer))
                .DisposeMany()
                .AsObservableCache();

            _imageViewModelCache = _imageContainerViewModelCache
                .Connect()
                .TransformMany(imageContainerViewModel =>
                        imageContainerViewModel.ImageRefs.Select(imageRef =>
                            new ImageViewModel(imageRef,
                                imageContainerViewModel)),
                    imageViewModel => imageViewModel.ImageRefId)
                .DisposeMany()
                .AsObservableCache();

            _trayImageSourceCache = new SourceCache<ImageViewModel, int>(model => model.ImageId);

            _trayImageCache = _trayImageSourceCache
                .Connect()
                .Transform(model => new TrayImageViewModel(model))
                .DisposeMany()
                .AsObservableCache();

            this.WhenActivated(d =>
            {
                _imageContainerManagementService
                    .Start()
                    .Subscribe();

                _selectedImagesSourceCache
                    .Connect()
                    .Subscribe(set =>
                    {
                        _trayImageSourceCache.Edit(updater =>
                        {
                            foreach (var change in set)
                                switch (change.Reason)
                                {
                                    case ChangeReason.Add:
                                        if (!updater.Lookup(change.Current.ImageId).HasValue)
                                            updater.AddOrUpdate(change.Current);
                                        break;

                                    case ChangeReason.Update:
                                        break;

                                    case ChangeReason.Remove:
                                        var lookup = _trayImageCache.Lookup(change.Current.ImageId);
                                        if (lookup.HasValue && !lookup.Value.Pinned)
                                            updater.Remove(change.Current.ImageId);
                                        break;
                                    case ChangeReason.Refresh:
                                        break;
                                    case ChangeReason.Moved:
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                        });
                    })
                    .DisposeWith(d);

                _imageContainerViewModelCache
                    .Connect()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(ImageContainers)
                    .Subscribe()
                    .DisposeWith(d);

                _imageContainerViewModelCache
                    .Connect()
                    .Filter(model => model.ContainerType == ImageContainerTypeEnum.Album)
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(AlbumImageContainers)
                    .Subscribe()
                    .DisposeWith(d);

                _imageViewModelCache
                    .Connect()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(Images)
                    .Subscribe()
                    .DisposeWith(d);

                _trayImageCache
                    .Connect()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(TrayImages)
                    .Subscribe()
                    .DisposeWith(d);

                this.WhenAny(model => model.ImagesViewportWidth, change => Math.Max(1, (int) (change.Value / 304)))
                    .ToProperty(this, nameof(ImagesViewportColumns), out _imagesViewportColumns);
            });
        }

        public IObservable<IEnumerable<ImageViewModel>> UnselectImage => _unselectImageSubject;

        public IObservable<IEnumerable<TrayImageViewModel>> UnselectTrayImage => _unselectTrayImageSubject;

        public Interaction<Unit, string> AddFolderInteraction { get; } = new Interaction<Unit, string>();

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; } =
            new Interaction<Unit, AddAlbumViewModel>();

        public Interaction<Unit, IManageFolderRulesViewModel> FolderManagerInteraction { get; } =
            new Interaction<Unit, IManageFolderRulesViewModel>();

        public Interaction<ResetChanges, bool> FolderManagerConfirmationInteraction { get; } =
            new Interaction<ResetChanges, bool>();

        public IObservableCollection<ImageContainerViewModel> ImageContainers { get; } =
            new ObservableCollectionExtended<ImageContainerViewModel>();

        public IObservableCollection<ImageContainerViewModel> AlbumImageContainers { get; } =
            new ObservableCollectionExtended<ImageContainerViewModel>();

        public IObservableCollection<ImageViewModel> Images { get; } =
            new ObservableCollectionExtended<ImageViewModel>();

        public IObservableCollection<TrayImageViewModel> TrayImages { get; } =
            new ObservableCollectionExtended<TrayImageViewModel>();

        public IObservableCollection<TrayImageViewModel> SelectedTrayImages { get; } =
            new ObservableCollectionExtended<TrayImageViewModel>();

        public ReactiveCommand<Unit, Unit> AddFolder { get; }

        public ReactiveCommand<Unit, ImageContainerViewModel> NewAlbum { get; }

        public ReactiveCommand<(IEnumerable<ImageViewModel>, ImageContainerViewModel), ImageContainerViewModel>
            AddImagesToAlbum { get; }

        public ReactiveCommand<IEnumerable<ImageViewModel>, ImageContainerViewModel> NewAlbumWithImages { get; }

        public ReactiveCommand<IEnumerable<TrayImageViewModel>, Unit> PinSelectedItems { get; }

        public ReactiveCommand<(IEnumerable<TrayImageViewModel>, bool), Unit> ClearTrayItems { get; }

        public Interaction<Unit, bool> ConfirmClearTrayItemsInteraction { get; } = new Interaction<Unit, bool>();

        public ReactiveCommand<Unit, Unit> AddTrayItemsToAlbum { get; }

        public ReactiveCommand<Unit, Unit> FolderManager { get; }

        public string VisibleItemContainerKey
        {
            get => _visibleItemContainerKey;
            set => this.RaiseAndSetIfChanged(ref _visibleItemContainerKey, value);
        }

        public double ImagesViewportWidth
        {
            get => _imagesViewportWidth;
            set => this.RaiseAndSetIfChanged(ref _imagesViewportWidth, value);
        }

        public int ImagesViewportColumns => _imagesViewportColumns.Value;

        public void Dispose()
        {
            _imageContainerViewModelCache?.Dispose();
            _imageViewModelCache?.Dispose();
            _selectedImagesSourceCache?.Dispose();
            _trayImageCache?.Dispose();
            _trayImageSourceCache?.Dispose();
            _unselectImageSubject?.Dispose();
            _unselectTrayImageSubject?.Dispose();
            _imagesViewportColumns?.Dispose();
        }

        private IObservable<ImageContainerViewModel> ExecuteNewAlbum(Unit _)
        {
            return NewAlbumInteraction.Handle(Unit.Default)
                .ObserveOn(_schedulerProvider.TaskPool)
                .Select(model =>
                {
                    if (model == null)
                        return Observable.Return((ImageContainerViewModel) null);

                    return _imageContainerManagementService.CreateAlbum(model)
                        .Select(imageContainer => _imageContainerViewModelCache.Lookup(imageContainer.Key).Value);
                })
                .SelectMany(observable => observable);
        }

        private IObservable<ImageContainerViewModel> ExecuteAddImagesToAlbum(
            (IEnumerable<ImageViewModel>, ImageContainerViewModel) tuple)
        {
            return Observable.Defer(() =>
            {
                var (imageViewModels, imageContainerViewModel) = tuple;

                var addImagesToAlbum = _imageContainerManagementService
                    .AddImagesToAlbum(imageContainerViewModel.ContainerTypeId,
                        imageViewModels.Select(viewModel => viewModel.ImageId))
                    .Select(imageContainer => _imageContainerViewModelCache.Lookup(imageContainer.Key).Value);

                return addImagesToAlbum;
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        private IObservable<ImageContainerViewModel> ExecuteNewAlbumWithImages(
            IEnumerable<ImageViewModel> imageViewModels)
        {
            return ExecuteNewAlbum(Unit.Default)
                .Select(model =>
                {
                    if (model == null) return Observable.Return((ImageContainerViewModel) null);

                    return ExecuteAddImagesToAlbum((imageViewModels, model));
                })
                .SelectMany(observable => observable);
        }

        private IObservable<Unit> ExecuteAddFolder(Unit unit)
        {
            return AddFolderInteraction.Handle(Unit.Default)
                .ObserveOn(_schedulerProvider.TaskPool)
                .Select(s =>
                {
                    return s != null
                        ? _imageContainerManagementService.ScanFolder(s)
                            .Select(container => Unit.Default)
                            .LastOrDefaultAsync()
                        : Observable.Return(Unit.Default);
                })
                .SelectMany(observable => observable);
        }

        private IObservable<Unit> ExecutePinSelectedItems(IEnumerable<TrayImageViewModel> trayImageViewModels)
        {
            return Observable.Start(() =>
            {
                foreach (var trayImageViewModel in trayImageViewModels) trayImageViewModel.Pinned = true;
            }, _schedulerProvider.MainThreadScheduler);
        }

        private IObservable<Unit> ExecuteClearTrayItems(
            (IEnumerable<TrayImageViewModel> trayImageViewModels, bool isAllItems) tuple)
        {
            return Observable.Start(() =>
                {
                    var (trayImageViewModels, isAllItems) = tuple;

                    if (isAllItems)
                        return ConfirmClearTrayItemsInteraction.Handle(Unit.Default)
                            .Select(b => (trayImageViewModels, b));

                    return Observable.Return((trayImageViewModels, true));
                }, _schedulerProvider.TaskPool)
                .SelectMany(observable => observable)
                .Select(valueTuple =>
                {
                    var (trayImageViewModels, shouldContinue) = valueTuple;
                    if (shouldContinue)
                    {
                        var trayImageViewModelsArray = trayImageViewModels.ToArray();
                        var imageIds = trayImageViewModelsArray.Select(model => model.ImageViewModel.ImageId);
                        _trayImageSourceCache.RemoveKeys(imageIds);
                        _unselectImageSubject.OnNext(trayImageViewModelsArray.Select(model => model.ImageViewModel));
                        _unselectTrayImageSubject.OnNext(trayImageViewModelsArray);
                    }

                    return Unit.Default;
                });
        }

        private IObservable<Unit> ExecuteAddTrayItemsToAlbum(Unit unit)
        {
            return Observable.Start(() => Unit.Default);
        }

        public void ChangeSelectedImages(IEnumerable<ImageViewModel> added, IEnumerable<ImageViewModel> removed)
        {
            _selectedImagesSourceCache.Edit(updater =>
            {
                updater.AddOrUpdate(added);
                updater.Remove(removed);
            });
        }

        private IObservable<Unit> ExecuteFolderManager(Unit unit)
        {
            return FolderManagerInteraction.Handle(Unit.Default)
                .ObserveOn(_schedulerProvider.TaskPool)
                .Select(folderManagementViewModel =>
                {
                    if (folderManagementViewModel != null)
                    {
                        var folderRuleInputs = folderManagementViewModel.Folders;

                        var folderRules = FolderRulesFactory.ComputeRuleset(folderRuleInputs)
                            .ToArray();

                        return _imageContainerManagementService
                            .PreviewResetRulesChanges(folderRules)
                            .ObserveOn(_schedulerProvider.MainThreadScheduler)
                            .SelectMany(resetChanges => FolderManagerConfirmationInteraction.Handle(resetChanges)
                                .ObserveOn(_schedulerProvider.TaskPool)
                                .SelectMany(b => b
                                    ? _imageContainerManagementService
                                        .ResetRules(folderRules)
                                        .Select(changes => Unit.Default)
                                        .LastOrDefaultAsync()
                                    : Observable.Return(Unit.Default)));
                    }

                    return Observable.Return(Unit.Default);
                })
                .SelectMany(observable => observable);
        }
    }
}