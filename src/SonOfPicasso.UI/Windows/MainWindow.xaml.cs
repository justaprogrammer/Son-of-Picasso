﻿using System;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using DynamicData;
using DynamicData.Binding;
using MoreLinq;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Extensions;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.ViewModels.FolderRules;
using SonOfPicasso.UI.Windows.Dialogs;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;

namespace SonOfPicasso.UI.Windows
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ReactiveWindow<ApplicationViewModel>
    {
        private readonly Func<AddAlbumViewModel> _addAlbumViewModelFactory;
        private readonly Func<AddAlbumWindow> _addAlbumWindowFactory;
        private readonly CollectionViewSource _albumImageContainersViewSource;
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystem _fileSystem;
        private readonly Func<ManageFolderRulesViewModel> _folderManagementViewModelFactory;
        private readonly Func<FolderManagementWindow> _folderManagementWindowFactory;
        private readonly CollectionViewSource _imageCollectionViewSource;
        private readonly CollectionViewSource _imageContainersViewSource;
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        public MainWindow(ILogger logger, IEnvironmentService environmentService, IFileSystem fileSystem,
            ISchedulerProvider schedulerProvider,
            IImageLoadingService imageLoadingService,
            Func<AddAlbumWindow> addAlbumWindowFactory,
            Func<AddAlbumViewModel> addAlbumViewModelFactory,
            Func<FolderManagementWindow> folderManagementWindowFactory,
            Func<ManageFolderRulesViewModel> folderManagementViewModelFactory)
        {
            _logger = logger;
            _environmentService = environmentService;
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
            _imageLoadingService = imageLoadingService;
            _addAlbumWindowFactory = addAlbumWindowFactory;
            _addAlbumViewModelFactory = addAlbumViewModelFactory;
            _folderManagementWindowFactory = folderManagementWindowFactory;
            _folderManagementViewModelFactory = folderManagementViewModelFactory;

            InitializeComponent();

            _imageCollectionViewSource = (CollectionViewSource) FindResource("ImagesCollectionViewSource");
            _imageContainersViewSource = (CollectionViewSource) FindResource("ImageContainersViewSource");
            _albumImageContainersViewSource = (CollectionViewSource) FindResource("AlbumImageContainersViewSource");

            this.WhenActivated(d =>
            {
                _imageCollectionViewSource.Source = ViewModel.Images;

                var propertyGroupDescription =
                    new PropertyGroupDescription(nameof(ImageViewModel.ImageContainerViewModel));

                _imageCollectionViewSource.GroupDescriptions.Add(propertyGroupDescription);
                _imageCollectionViewSource.SortDescriptions.Add(new SortDescription(
                    nameof(ImageViewModel.ContainerType),
                    ListSortDirection.Ascending));
                _imageCollectionViewSource.SortDescriptions.Add(new SortDescription(
                    nameof(ImageViewModel.ContainerDate),
                    ListSortDirection.Descending));
                _imageCollectionViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageViewModel.ExifDate),
                    ListSortDirection.Ascending));

                _imageCollectionViewSource.IsLiveFilteringRequested = true;
                _imageCollectionViewSource.IsLiveGroupingRequested = true;
                _imageCollectionViewSource.IsLiveSortingRequested = true;

                _imageContainersViewSource.Source = ViewModel.ImageContainers;
                _imageContainersViewSource.GroupDescriptions.Add(
                    new PropertyGroupDescription(nameof(ImageContainerViewModel.ContainerType)));
                _imageContainersViewSource.GroupDescriptions.Add(
                    new PropertyGroupDescription(nameof(ImageContainerViewModel.Year)));
                _imageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.ContainerType),
                        ListSortDirection.Ascending));
                _imageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

                _imageContainersViewSource.IsLiveFilteringRequested = true;
                _imageContainersViewSource.IsLiveGroupingRequested = true;
                _imageContainersViewSource.IsLiveSortingRequested = true;

                _albumImageContainersViewSource.Source = ViewModel.AlbumImageContainers;
                _albumImageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.Year), ListSortDirection.Descending));
                _albumImageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

                _albumImageContainersViewSource.IsLiveFilteringRequested = true;
                _albumImageContainersViewSource.IsLiveGroupingRequested = true;
                _albumImageContainersViewSource.IsLiveSortingRequested = true;

                ImagesListScrollViewer = ImagesList.FindVisualChildren<ScrollViewer>().First();

                ContainersList.Events()
                    .SelectionChanged
                    .Subscribe(args =>
                    {
                        var imageContainerViewModel = args.AddedItems
                            .Cast<ImageContainerViewModel>()
                            .FirstOrDefault();

                        if (imageContainerViewModel == null) return;

                        var (groupIndex, rowIndex) = _imageCollectionViewSource
                            .View
                            .Groups
                            .Cast<CollectionViewGroup>()
                            .SelectMany((group, g) => group.Items
                                .Cast<ImageViewModel>()
                                .Batch(ViewModel.ImagesViewportColumns)
                                .Select(models => (models, g)))
                            .Select((tuple, r) => (tuple.models, tuple.g, r))
                            .Where(tuple =>
                                tuple.models.Any(model => model.ContainerKey == imageContainerViewModel.ContainerKey))
                            .Select(tuple => (tuple.g, tuple.r))
                            .FirstOrDefault();

                        ImagesListScrollViewer.ScrollToVerticalOffset(rowIndex * 304 + groupIndex * 25.96);
                    });

                Observable.Create<string>(observer =>
                    {
                        var disposable1 = _imageCollectionViewSource
                            .View
                            .ObserveCollectionChanges()
                            .Select(pattern => (ICollectionView) pattern.Sender)
                            .Select(view => (CollectionViewGroup) view.Groups.FirstOrDefault())
                            .Select(group => (ImageViewModel) group?.Items.FirstOrDefault())
                            .DistinctUntilChanged(model => model?.ContainerKey)
                            .Subscribe(imageViewModel => { observer.OnNext(imageViewModel?.ContainerKey); });

                        var disposable2 = ImagesListScrollViewer
                            .WhenAny(
                                scrollViewer => scrollViewer.VerticalOffset,
                                observedChange1 => observedChange1.Value)
                            .Skip(1)
                            .DistinctUntilChanged(verticalOffset => (int) (verticalOffset / 50))
                            .Select(verticalOffset =>
                            {
                                var listViewItem = GetFirstVisibleListViewItem<ImageViewModel>(ImagesListScrollViewer);
                                var imageViewModel1 = (ImageViewModel) listViewItem?.DataContext;
                                return imageViewModel1?.ImageContainerViewModel;
                            })
                            .DistinctUntilChanged()
                            .Subscribe(model =>
                            {
                                if (disposable1 != null)
                                {
                                    disposable1.Dispose();
                                    disposable1 = null;
                                }

                                observer.OnNext(model?.ContainerKey);
                            });

                        return new CompositeDisposable(disposable1, disposable2);
                    })
                    .BindTo(ViewModel, model => model.VisibleItemContainerKey);

                ImagesListScrollViewer
                    .WhenAny(
                        scrollViewer => scrollViewer.ViewportWidth,
                        change => change.Value)
                    .Subscribe(value => ViewModel.ImagesViewportWidth = value)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        model => model.AddFolder,
                        window => window.AddFolder)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        model => model.NewAlbum,
                        window => window.NewAlbum)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        model => model.FolderManager,
                        window => window.FolderManager)
                    .DisposeWith(d);

                var observableSelectedTrayImageCount = ViewModel.WhenAnyValue(
                    model => model.TrayImages.Count,
                    model => model.SelectedTrayImages.Count,
                    (trayImages, selectedTrayImages) => selectedTrayImages);

                this.BindCommand(ViewModel,
                        model => model.PinSelectedItems,
                        window => window.PinSelectedItems,
                        observableSelectedTrayImageCount
                            .Select(selectedTrayCount =>
                                (selectedTrayCount == 0 ? ViewModel.TrayImages : ViewModel.SelectedTrayImages)
                                .AsEnumerable()))
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                    model => model.ClearTrayItems,
                    window => window.ClearTrayItems,
                    observableSelectedTrayImageCount
                        .Select(selectedTrayCount =>
                        {
                            var allItems = selectedTrayCount == 0;

                            var collection =
                                !allItems ? ViewModel.SelectedTrayImages : ViewModel.TrayImages;

                            return (collection.AsEnumerable(), allItems);
                        })).DisposeWith(d);

                ImagesList.Events().SelectionChanged
                    .Subscribe(ea =>
                    {
                        ViewModel.ChangeSelectedImages(
                            ea.AddedItems.Cast<ImageViewModel>(),
                            ea.RemovedItems.Cast<ImageViewModel>());
                    }).DisposeWith(d);

                ViewModel.UnselectImage
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Subscribe(imageViewModels =>
                    {
                        foreach (var imageViewModel in imageViewModels)
                            ImagesList.SelectedItems.Remove(imageViewModel);
                    })
                    .DisposeWith(d);

                TrayImagesList.Events().SelectionChanged.Subscribe(ea =>
                {
                    using (ViewModel.SelectedTrayImages.SuspendNotifications())
                    {
                        ViewModel.SelectedTrayImages.RemoveMany(ea.RemovedItems.Cast<TrayImageViewModel>());
                        ViewModel.SelectedTrayImages.AddRange(ea.AddedItems.Cast<TrayImageViewModel>());
                    }
                }).DisposeWith(d);

                ViewModel.UnselectTrayImage
                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
                    .Subscribe(trayImageViewModels =>
                    {
                        foreach (var trayImageViewModel in trayImageViewModels)
                            TrayImagesList.SelectedItems.Remove(trayImageViewModel);
                    })
                    .DisposeWith(d);

                this.OneWayBind(ViewModel,
                    model => model.TrayImages,
                    window => window.TrayImagesList.ItemsSource).DisposeWith(d);

                ViewModel.NewAlbumInteraction.RegisterHandler(context =>
                {
                    return Observable.Defer(() =>
                    {
                        var addAlbumWindow = _addAlbumWindowFactory();
                        var addAlbumViewModel = _addAlbumViewModelFactory();

                        addAlbumWindow.ViewModel = addAlbumViewModel;

                        AddAlbumViewModel result = null;
                        if (addAlbumWindow.ShowDialog() == true) result = addAlbumWindow.ViewModel;

                        context.SetOutput(result);
                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(_schedulerProvider.MainThreadScheduler);
                }).DisposeWith(d);

                ViewModel.AddFolderInteraction.RegisterHandler(context =>
                {
                    return Observable.Defer(() =>
                    {
                        var dialog = new FolderBrowserDialog
                        {
                            SelectedPath = _environmentService.GetFolderPath(Environment.SpecialFolder.MyPictures)
                        };

                        string result = null;
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) result = dialog.SelectedPath;

                        context.SetOutput(result);
                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(_schedulerProvider.MainThreadScheduler);
                }).DisposeWith(d);

                ViewModel.ConfirmClearTrayItemsInteraction.RegisterHandler(context =>
                {
                    return Observable.Defer(() =>
                    {
                        var messageBoxResult = MessageBox.Show(
                            "This will clear items in the tray. Are you sure?",
                            "Confirmation", MessageBoxButton.YesNo);
                        context.SetOutput(messageBoxResult == MessageBoxResult.Yes);

                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(_schedulerProvider.MainThreadScheduler);
                }).DisposeWith(d);

                ViewModel.FolderManagerInteraction.RegisterHandler(context =>
                    {
                        return Observable.Defer(async () =>
                        {
                            var folderManagementWindow = _folderManagementWindowFactory();
                            var folderManagementViewModel = _folderManagementViewModelFactory();

                            using (_logger.BeginTimedOperation("Initializing FolderManagementViewModel",
                                level: LogEventLevel.Debug))
                            {
                                await folderManagementViewModel.Initialize();
                            }

                            folderManagementWindow.ViewModel = folderManagementViewModel;

                            ManageFolderRulesViewModel result = null;
                            if (folderManagementWindow.ShowDialog() == true) result = folderManagementWindow.ViewModel;

                            context.SetOutput(result);
                            return Observable.Return(Unit.Default);
                        }).SubscribeOn(_schedulerProvider.MainThreadScheduler);
                    })
                    .DisposeWith(d);

                ViewModel.FolderManagerConfirmationInteraction.RegisterHandler(context =>
                {
                    return Observable.Defer(() =>
                    {
                        var deletedCount = context.Input.DeletedImagePaths.Length;
                        var imagesString = deletedCount == 0 ? "image" : "images";

                        var messageBoxResult = MessageBox.Show(
                            $"This action will remove {deletedCount} {imagesString} from the database. Are you sure?",
                            "Confirmation", MessageBoxButton.YesNo);

                        context.SetOutput(messageBoxResult == MessageBoxResult.Yes);

                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(_schedulerProvider.MainThreadScheduler);
                });
            });
        }

        public ScrollViewer ImagesListScrollViewer { get; set; }

        private ListViewItem GetFirstVisibleListViewItem<TDataContextType>(ScrollViewer imagesListScrollViewer)
        {
            var listViewItems = ImagesListScrollViewer
                .FindVisualChildren<ListViewItem>()
                .ToArray();

            ListViewItem lastListViewItem = null;
            Point lastViewViewItemPoint;

            foreach (var listViewItem in listViewItems)
            {
                if (!listViewItem.DataContext.GetType().Equals(typeof(TDataContextType)))
                    continue;

                var translatePoint = listViewItem.TranslatePoint(new Point(), imagesListScrollViewer);

                if (translatePoint.Y <= 0)
                {
                    if (lastListViewItem == null || lastViewViewItemPoint.Y != translatePoint.Y)
                    {
                        lastListViewItem = listViewItem;
                        lastViewViewItemPoint = translatePoint;
                    }

                    continue;
                }

                if (lastListViewItem == null) lastListViewItem = listViewItem;

                break;
            }

            return lastListViewItem;
        }

        private void AlbumButton_AddImagesToAlbum_OnClick(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem) sender;
            var imageContainerViewModel = (ImageContainerViewModel) menuItem.DataContext;

            var viewModelSelectedTrayImages =
                ViewModel.SelectedTrayImages.Any() ? ViewModel.SelectedTrayImages : ViewModel.TrayImages;

            var imageViewModels =
                viewModelSelectedTrayImages.Select(model => model.ImageViewModel).ToList();

            ViewModel.AddImagesToAlbum.Execute((imageViewModels.AsEnumerable(), imageContainerViewModel))
                .Subscribe();
        }

        private void AlbumButton_AddAlbum_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModelSelectedTrayImages =
                ViewModel.SelectedTrayImages.Any() ? ViewModel.SelectedTrayImages : ViewModel.TrayImages;

            var imageViewModels =
                viewModelSelectedTrayImages.Select(model => model.ImageViewModel).ToList();

            ViewModel.NewAlbumWithImages.Execute(imageViewModels)
                .Subscribe();
        }

        private void ImageBitmap_OnInitialized(object sender, EventArgs e)
        {
            var image = (Image) sender;
            var imageViewModel = (ImageViewModel) image.DataContext;
            _imageLoadingService.LoadThumbnailFromPath(imageViewModel.Path)
                .ObserveOnDispatcher()
                .Subscribe(source => image.Source = source);
        }
    }
}