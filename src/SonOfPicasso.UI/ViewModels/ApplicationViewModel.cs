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
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels.Abstract;

namespace SonOfPicasso.UI.ViewModels
{
    public class ApplicationViewModel : ViewModelBase, IDisposable
    {
        private readonly SourceCache<ImageContainer, string> _imageContainerCache;
        private readonly IObservableCache<ImageContainerViewModel, string> _imageContainerViewModelCache;

        private readonly Func<ImageContainerViewModel> _imageContainerViewModelFactory;
        private readonly IImageManagementService _imageManagementService;
        private readonly IObservableCache<ImageViewModel, string> _imageViewModelCache;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly Func<ImageViewModel> _imageViewModelFactory;
        private readonly ISchedulerProvider _schedulerProvider;

        private readonly SourceCache<ImageViewModel, int> _selectedImagesSourceCache;
        private readonly IObservableCache<TrayImageViewModel, int> _trayImageCache;

        private readonly SourceCache<ImageViewModel, int> _trayImageSourceCache;
        private readonly Func<TrayImageViewModel> _trayImageViewModelFactory;
        private readonly Subject<IEnumerable<ImageViewModel>> _unselectImageSubject = new Subject<IEnumerable<ImageViewModel>>();
        private readonly Subject<IEnumerable<TrayImageViewModel>> _unselectTrayImageSubject = new Subject<IEnumerable<TrayImageViewModel>>();

        public ApplicationViewModel(ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            IFolderRulesManagementService folderRulesManagementService,
            Func<ImageContainerViewModel> imageContainerViewModelFactory,
            Func<ImageViewModel> imageViewModelFactory,
            Func<TrayImageViewModel> trayImageViewModelFactory,
            ViewModelActivator activator) : base(activator)
        {
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _folderRulesManagementService = folderRulesManagementService;
            _imageContainerViewModelFactory = imageContainerViewModelFactory;
            _imageViewModelFactory = imageViewModelFactory;
            _trayImageViewModelFactory = trayImageViewModelFactory;

            _selectedImagesSourceCache = new SourceCache<ImageViewModel, int>(model => model.ImageId);

            FolderManager = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteFolderManager);
            AddFolder = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddFolder);
            NewAlbum = ReactiveCommand.CreateFromObservable<Unit, ImageContainerViewModel>(ExecuteNewAlbum);
            NewAlbumWithImages = ReactiveCommand.CreateFromObservable<IEnumerable<ImageViewModel>, ImageContainerViewModel>(ExecuteNewAlbumWithImages);
            AddImagesToAlbum = ReactiveCommand.CreateFromObservable<(IEnumerable<ImageViewModel>, ImageContainerViewModel), ImageContainerViewModel>(ExecuteAddImagesToAlbum);

            var hasItemsInTray = this.WhenAnyValue(model => model.TrayImages.Count)
                .Select(propertyValue => propertyValue > 0);

            PinSelectedItems =
                ReactiveCommand.CreateFromObservable<IEnumerable<TrayImageViewModel>, Unit>(ExecutePinSelectedItems,
                    hasItemsInTray);
            ClearTrayItems =
                ReactiveCommand.CreateFromObservable<(IEnumerable<TrayImageViewModel>, bool), Unit>(ExecuteClearTrayItems,
                    hasItemsInTray);
            AddTrayItemsToAlbum =
                ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddTrayItemsToAlbum, hasItemsInTray);

            _imageContainerCache = new SourceCache<ImageContainer, string>(imageContainer => imageContainer.Id);

            _imageContainerViewModelCache = _imageContainerCache
                .Connect()
                .Transform(CreateImageContainerViewModel)
                .DisposeMany()
                .AsObservableCache();

            _imageViewModelCache = _imageContainerViewModelCache
                .Connect()
                .TransformMany(CreateImageViewModels, imageViewModel => imageViewModel.ImageRefId)
                .DisposeMany()
                .AsObservableCache();

            _trayImageSourceCache = new SourceCache<ImageViewModel, int>(model => model.ImageId);

            _trayImageCache = _trayImageSourceCache
                .Connect()
                .Transform(CreateTrayImageViewModel)
                .DisposeMany()
                .AsObservableCache();

            this.WhenActivated(d =>
            {
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

                _imageContainerCache
                    .PopulateFrom(_imageManagementService.GetAllImageContainers().ToArray())
                    .DisposeWith(d);
            });
        }

        public IObservable<IEnumerable<ImageViewModel>> UnselectImage => _unselectImageSubject;
        
        public IObservable<IEnumerable<TrayImageViewModel>> UnselectTrayImage => _unselectTrayImageSubject;

        public Interaction<Unit, string> AddFolderInteraction { get; set; } = new Interaction<Unit, string>();

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; set; } =
            new Interaction<Unit, AddAlbumViewModel>();

        public Interaction<Unit, ManageFolderRulesViewModel> FolderManagerInteraction { get; set; } =
            new Interaction<Unit, ManageFolderRulesViewModel>();

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

        public ReactiveCommand<(IEnumerable<ImageViewModel>, ImageContainerViewModel), ImageContainerViewModel> AddImagesToAlbum { get; set; }
    
        public ReactiveCommand<IEnumerable<ImageViewModel>, ImageContainerViewModel> NewAlbumWithImages { get; }

        public ReactiveCommand<IEnumerable<TrayImageViewModel>, Unit> PinSelectedItems { get; }

        public ReactiveCommand<(IEnumerable<TrayImageViewModel>, bool), Unit> ClearTrayItems { get; }

        public Interaction<Unit, bool> ConfirmClearTrayItemsInteraction { get; set; } = new Interaction<Unit, bool>();

        public ReactiveCommand<Unit, Unit> AddTrayItemsToAlbum { get; }

        public ReactiveCommand<Unit, Unit> FolderManager { get; }

        public void Dispose()
        {
            _imageContainerCache?.Dispose();
        }

        private IEnumerable<ImageViewModel> CreateImageViewModels(ImageContainerViewModel containerViewModel)
        {
            return containerViewModel.ImageRefs.Select(imageRef =>
                CreateImageViewModel(imageRef, containerViewModel));
        }

        private TrayImageViewModel CreateTrayImageViewModel(ImageViewModel model)
        {
            var trayImageViewModel = _trayImageViewModelFactory();
            trayImageViewModel.Initialize(model);
            return trayImageViewModel;
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

        private IObservable<ImageContainerViewModel> ExecuteNewAlbum(Unit _)
        {
            return NewAlbumInteraction.Handle(Unit.Default)
                .ObserveOn(_schedulerProvider.TaskPool)
                .Select(model =>
                {
                    if (model == null)
                        return Observable.Return((ImageContainerViewModel) null);

                    return _imageManagementService.CreateAlbum(model)
                        .Select(imageContainer =>
                        {
                            _imageContainerCache.AddOrUpdate(imageContainer);
                            return _imageContainerViewModelCache.Lookup(imageContainer.Id).Value;
                        });
                })
                .SelectMany(observable => observable);
        }

        private IObservable<ImageContainerViewModel> ExecuteAddImagesToAlbum((IEnumerable<ImageViewModel>, ImageContainerViewModel) tuple)
        {
            return Observable.Defer(() =>
            {
                var (imageViewModels, imageContainerViewModel) = tuple;

                var addImagesToAlbum = _imageManagementService
                    .AddImagesToAlbum(imageContainerViewModel.ContainerTypeId,
                        imageViewModels.Select(viewModel => viewModel.ImageId))
                    .Select(imageContainer =>
                    {
                        _imageContainerCache.AddOrUpdate(imageContainer);
                        return _imageContainerViewModelCache.Lookup(imageContainer.Id).Value;
                    });

                return addImagesToAlbum;
            }).SubscribeOn(_schedulerProvider.TaskPool);
        }

        private IObservable<ImageContainerViewModel> ExecuteNewAlbumWithImages(IEnumerable<ImageViewModel> imageViewModels)
        {
            return ExecuteNewAlbum(Unit.Default)
                .Select(model =>
                {
                    if (model == null)
                    {
                        return Observable.Return((ImageContainerViewModel) null);
                    }

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
                    if (s != null)
                    {
                        _imageContainerCache.PopulateFrom(_imageManagementService.ScanFolder(s));
                    }

                    return Observable.Return(Unit.Default);
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
                        var imageIds = trayImageViewModelsArray.Select(model => model.Image.ImageId);
                        _trayImageSourceCache.RemoveKeys(imageIds);
                        _unselectImageSubject.OnNext(trayImageViewModelsArray.Select(model => model.Image));
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
                        return _folderRulesManagementService.ResetFolderManagementRules(folderManagementViewModel.Folders);
                    }

                    return Observable.Return(Unit.Default);
                })
                .SelectMany(observable => observable);
        }
    }
}