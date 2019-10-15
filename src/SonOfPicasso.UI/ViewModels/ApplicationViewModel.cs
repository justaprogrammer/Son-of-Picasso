using System;
using System.Collections.Generic;
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
        private readonly Func<TrayImageViewModel> _trayImageViewModelFactory;

        public ApplicationViewModel(ISchedulerProvider schedulerProvider,
            IImageManagementService imageManagementService,
            Func<ImageContainerViewModel> imageContainerViewModelFactory,
            Func<ImageViewModel> imageViewModelFactory,
            Func<TrayImageViewModel> trayImageViewModelFactory,
            ViewModelActivator activator) : base(activator)
        {
            _schedulerProvider = schedulerProvider;
            _imageManagementService = imageManagementService;
            _imageContainerViewModelFactory = imageContainerViewModelFactory;
            _imageViewModelFactory = imageViewModelFactory;
            _trayImageViewModelFactory = trayImageViewModelFactory;

            var imageContainers = new ObservableCollectionExtended<ImageContainerViewModel>();
            ImageContainers = imageContainers;

            var images = new ObservableCollectionExtended<ImageViewModel>();
            Images = images;

            var selectedImages = new ObservableCollectionExtended<ImageViewModel>();
            SelectedImages = selectedImages;

            var trayImages = new ObservableCollectionExtended<TrayImageViewModel>();
            TrayImages = trayImages;

            var selectedTrayImages = new ObservableCollectionExtended<TrayImageViewModel>();
            SelectedTrayImages = selectedTrayImages;

            AddFolderInteraction = new Interaction<Unit, string>();
            NewAlbumInteraction = new Interaction<Unit, AddAlbumViewModel>();

            AddFolder = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddFolder);
            NewAlbum = ReactiveCommand.CreateFromObservable(ExecuteNewAlbum);
                
            var hasItemsInTray = this.WhenAnyValue(model => model.TrayImages.Count)
                .Select(propertyValue => propertyValue > 0);

            PinSelectedItems = ReactiveCommand.CreateFromObservable<IList<TrayImageViewModel>, Unit>(ExecutePinSelectedItems, hasItemsInTray);
            ClearTrayItems = ReactiveCommand.CreateFromObservable< (IList<TrayImageViewModel>, bool), Unit>(ExecuteClearTrayItems, hasItemsInTray);
            AddTrayItemsToAlbum = ReactiveCommand.CreateFromObservable<Unit, Unit>(ExecuteAddTrayItemsToAlbum, hasItemsInTray);

            ClearTrayItemsInteraction = new Interaction<Unit, Unit>();

            ImageContainerCache = new SourceCache<ImageContainer, string>(imageContainer => imageContainer.Id);

            TrayImageSourceCache = new SourceCache<ImageViewModel, int>(model => model.ImageId);

            TrayImageCache = TrayImageSourceCache
                .Connect()
                .Transform(CreateTrayImageViewModel)
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .Bind(trayImages)
                .AsObservableCache();

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
                    .Bind(imageContainers)
                    .Subscribe()
                    .DisposeWith(d);

                imageContainerViewModelCache
                    .Connect()
                    .TransformMany(CreateImageViewModels, imageViewModel => imageViewModel.ImageRefId)
                    .DisposeMany()
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Bind(images)
                    .Subscribe()
                    .DisposeWith(d);

                selectedImages
                    .ToObservableChangeSet()
                    .Subscribe(set =>
                    {
                        TrayImageSourceCache.Edit(updater =>
                        {
                            foreach (var change in set)
                            {
                                switch (change.Reason)
                                {
                                    case ListChangeReason.AddRange:
                                        updater.AddOrUpdate(change.Range);
                                        break;

                                    case ListChangeReason.RemoveRange:
                                    case ListChangeReason.Clear:
                                        foreach (var imageViewModel in change.Range)
                                        {
                                            var lookup = TrayImageCache.Lookup(imageViewModel.ImageId);
                                            if (lookup.HasValue && !lookup.Value.Pinned)
                                            {
                                                updater.Remove(imageViewModel.ImageId);
                                            }
                                        }
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }
                        });
                    })
                    .DisposeWith(d);

                ImageContainerCache
                    .PopulateFrom(_imageManagementService.GetAllImageContainers().ToArray())
                    .DisposeWith(d);
            });
        }

        public Interaction<Unit, string> AddFolderInteraction { get; set; }

        public Interaction<Unit, AddAlbumViewModel> NewAlbumInteraction { get; set; }

        private SourceCache<ImageContainer, string> ImageContainerCache { get; }
        
        private SourceCache<ImageViewModel, int> TrayImageSourceCache { get; }
        
        private IObservableCache<TrayImageViewModel, int> TrayImageCache { get; }

        public ObservableCollectionExtended<ImageContainerViewModel> ImageContainers { get; }

        public ObservableCollectionExtended<ImageViewModel> Images { get; }

        public ObservableCollectionExtended<ImageViewModel> SelectedImages { get; }
        
        public ObservableCollectionExtended<TrayImageViewModel> TrayImages { get; }

        public ObservableCollectionExtended<TrayImageViewModel> SelectedTrayImages { get; }

        public ReactiveCommand<Unit, Unit> AddFolder { get; private set; }

        public ReactiveCommand<Unit, Unit> NewAlbum { get; private set; }
        
        public ReactiveCommand<IList<TrayImageViewModel>, Unit> PinSelectedItems { get; private set; }
        
        public ReactiveCommand<(IList<TrayImageViewModel>, bool), Unit> ClearTrayItems { get; private set; }

        public Interaction<Unit, Unit> ClearTrayItemsInteraction { get; set; }

        public ReactiveCommand<Unit, Unit> AddTrayItemsToAlbum { get; private set; }

        public void Dispose()
        {
            ImageContainerCache?.Dispose();
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
                    if (s != null) ImageContainerCache.PopulateFrom(_imageManagementService.ScanFolder(s).ToArray());

                    return Observable.Return(Unit.Default);
                })
                .SelectMany(observable => observable);
        }

        private IObservable<Unit> ExecutePinSelectedItems(IList<TrayImageViewModel> trayImageViewModels)
        {
            return Observable.Start(() =>
            {
                foreach (var trayImageViewModel in trayImageViewModels)
                {
                    trayImageViewModel.Pinned = true;
                }
            }, _schedulerProvider.MainThreadScheduler);
        }

        private IObservable<Unit> ExecuteClearTrayItems((IList<TrayImageViewModel> trayImageViewModels, bool isAllItems) tuple)
        {
            return Observable.Start(() =>
            {
                var (trayImageViewModels, isAllItems) = tuple;

                foreach (var trayImageViewModel in trayImageViewModels)
                {
                    trayImageViewModel.Pinned = false;
                }
            }, _schedulerProvider.MainThreadScheduler);
        }

        private IObservable<Unit> ExecuteAddTrayItemsToAlbum(Unit unit)
        {
            return Observable.Start(() => Unit.Default);
        }
    }
}