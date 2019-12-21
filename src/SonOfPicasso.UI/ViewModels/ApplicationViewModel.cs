using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IObservableCache<ImageContainerViewModel, string> _imageContainerViewModelCache;
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ILogger _logger;
        private readonly MemoryCache _memoryCache;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly SourceCache<ImageViewModel, int> _selectedImagesSourceCache;
        private readonly IObservableCache<TrayImageViewModel, int> _trayImageCache;
        private readonly SourceCache<ImageViewModel, int> _trayImageSourceCache;

        public ApplicationViewModel(ISchedulerProvider schedulerProvider,
            IImageContainerManagementService imageContainerManagementService,
            IImageLoadingService imageLoadingService,
            ViewModelActivator activator,
            ILogger logger) : base(activator)
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _schedulerProvider = schedulerProvider;
            _imageContainerManagementService = imageContainerManagementService;
            _imageLoadingService = imageLoadingService;
            _logger = logger;

            _selectedImagesSourceCache = new SourceCache<ImageViewModel, int>(model => model.ImageId);

            OpenFolderManager =
                ReactiveCommand.CreateFromObservable<Unit, Unit>(
                    ExecuteOpenFolderManager);

            AddFolder =
                ReactiveCommand.CreateFromObservable<Unit, Unit>(
                    ExecuteAddFolder);

            AddNewAlbum =
                ReactiveCommand.CreateFromObservable<Unit, ImageContainerViewModel>(
                    ExecuteAddNewAlbum);

            AddNewAlbumWithImages = ReactiveCommand
                .CreateFromObservable<IEnumerable<ImageViewModel>, ImageContainerViewModel>(
                    ExecuteAddNewAlbumWithImages);

            AddImagesToAlbum = ReactiveCommand
                .CreateFromObservable<(IEnumerable<ImageViewModel>, ImageContainerViewModel),
                    ImageContainerViewModel>(ExecuteAddImagesToAlbum);

            var hasItemsInTray = this.WhenAnyValue(model => model.TrayImages.Count)
                .Select(propertyValue => propertyValue > 0);

            PinSelectedItems =
                ReactiveCommand.CreateFromObservable<IEnumerable<TrayImageViewModel>, Unit>(ExecutePinSelectedItems,
                    hasItemsInTray);

            ClearTrayItems =
                ReactiveCommand.CreateFromObservable<(IEnumerable<TrayImageViewModel>, bool), IList<ImageViewModel>>(
                    ExecuteClearTrayItems,
                    hasItemsInTray);

            AddTrayItemsToAlbum =
                ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddTrayItemsToAlbum, hasItemsInTray);

            _imageContainerViewModelCache = _imageContainerManagementService
                .ImageContainerCache
                .Connect()
                .Transform(imageContainer => new ImageContainerViewModel(imageContainer, this))
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
                    .Subscribe()
                    .DisposeWith(d);

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

                _trayImageCache
                    .Connect()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(TrayImages)
                    .Subscribe()
                    .DisposeWith(d);
            });
        }

        public Interaction<Unit, string> AddFolderInteraction { get; } =
            new Interaction<Unit, string>();

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

        public Interaction<Unit, bool> ConfirmClearTrayItemsInteraction { get; } =
            new Interaction<Unit, bool>();

        public void Dispose()
        {
            _imageContainerViewModelCache?.Dispose();
        }

        public IObservable<BitmapSource> GetBitmapSourceFromPath(string path)
        {
            return _memoryCache.GetOrCreateAsync(path, async entry =>
            {
                var bitmapSource = await _imageLoadingService.LoadThumbnailFromPath((string) entry.Key)
                    .ObserveOn(_schedulerProvider.TaskPool);

                entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
                return bitmapSource;
            }).ToObservable();
        }

        public void ChangeSelectedImages(IEnumerable<ImageViewModel> added, IEnumerable<ImageViewModel> removed)
        {
            _selectedImagesSourceCache.Edit(updater =>
            {
                updater.AddOrUpdate(added);

                if (SelectedImageContainer != null)
                {
                    updater.AddOrUpdate(SelectedImageContainer.ImageViewModels);
                    SelectedImageContainer = null;
                }

                updater.Remove(removed);
            });
        }

        #region SelectedImageContainer

        private ImageContainerViewModel _selectedImageContainer;

        public ImageContainerViewModel SelectedImageContainer
        {
            get => _selectedImageContainer;
            set
            {
                if (TrayImages.All(model => !model.Pinned))
                {
                    var setValue = this.RaiseAndSetIfChanged(ref _selectedImageContainer, value);
                    if(setValue != null)
                    {
                        _selectedImagesSourceCache.Clear();
                    }
                }
            }
        }

        #endregion

        #region AddImagesToAlbum Command

        public ReactiveCommand<(IEnumerable<ImageViewModel>, ImageContainerViewModel), ImageContainerViewModel>
            AddImagesToAlbum { get; }

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

        #endregion

        #region AddFolder Command

        public ReactiveCommand<Unit, Unit> AddFolder { get; }

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

        #endregion

        #region ClearTrayItems Command

        public ReactiveCommand<(IEnumerable<TrayImageViewModel>, bool), IList<ImageViewModel>> ClearTrayItems { get; }

        private IObservable<IList<ImageViewModel>> ExecuteClearTrayItems(
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
                        var imageViewModelsArray = trayImageViewModels.Select(model => model.ImageViewModel).ToArray();
                        var imageIds = trayImageViewModelsArray.Select(model => model.ImageViewModel.ImageId);
                        _trayImageSourceCache.RemoveKeys(imageIds);

                        return imageViewModelsArray;
                    }

                    return Array.Empty<ImageViewModel>();
                });
        }

        #endregion

        #region AddTrayItemsToAlbum Command

        public ReactiveCommand<Unit, Unit> AddTrayItemsToAlbum { get; }

        private IObservable<Unit> ExecuteAddTrayItemsToAlbum(Unit unit)
        {
            return Observable.Start(() => Unit.Default);
        }

        #endregion

        #region PinSelectedItems Command

        public ReactiveCommand<IEnumerable<TrayImageViewModel>, Unit> PinSelectedItems { get; }

        private IObservable<Unit> ExecutePinSelectedItems(IEnumerable<TrayImageViewModel> trayImageViewModels)
        {
            return Observable.Start(() =>
            {
                foreach (var trayImageViewModel in trayImageViewModels) trayImageViewModel.Pinned = true;
            }, _schedulerProvider.MainThreadScheduler);
        }

        #endregion

        #region OpenFolderManager Command

        public ReactiveCommand<Unit, Unit> OpenFolderManager { get; }

        private IObservable<Unit> ExecuteOpenFolderManager(Unit unit)
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

        #endregion

        #region AddNewAlbum Command

        public ReactiveCommand<Unit, ImageContainerViewModel> AddNewAlbum { get; }

        private IObservable<ImageContainerViewModel> ExecuteAddNewAlbum(Unit _)
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

        #endregion

        #region AddNewAlbumWithImages Command

        public ReactiveCommand<IEnumerable<ImageViewModel>, ImageContainerViewModel> AddNewAlbumWithImages { get; }

        private IObservable<ImageContainerViewModel> ExecuteAddNewAlbumWithImages(
            IEnumerable<ImageViewModel> imageViewModels)
        {
            return ExecuteAddNewAlbum(Unit.Default)
                .Select(model =>
                {
                    if (model == null) return Observable.Return((ImageContainerViewModel) null);

                    return ExecuteAddImagesToAlbum((imageViewModels, model));
                })
                .SelectMany(observable => observable);
        }

        #endregion
    }
}