using System;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly CollectionViewSource _groupedImageContainersViewSource;
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly CollectionViewSource _imageContainersViewSource;

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

            _imageContainersViewSource = (CollectionViewSource) FindResource("ImageContainersViewSource");
            _groupedImageContainersViewSource = (CollectionViewSource) FindResource("GroupedImageContainersViewSource");
            _albumImageContainersViewSource = (CollectionViewSource) FindResource("AlbumImageContainersViewSource");

            ImageZoomSlider.Events().MouseDoubleClick.Subscribe(args => { ImageZoomSlider.Value = 100; });

            this.WhenActivated(d =>
            {
                _imageContainersViewSource.Source = ViewModel.ImageContainers;
                _imageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.ContainerType), ListSortDirection.Ascending));
                _imageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

                _imageContainersViewSource.IsLiveFilteringRequested = true;
                _imageContainersViewSource.IsLiveGroupingRequested = true;
                _imageContainersViewSource.IsLiveSortingRequested = true;

                _groupedImageContainersViewSource.Source = ViewModel.ImageContainers;
                _groupedImageContainersViewSource.GroupDescriptions.Add(
                    new PropertyGroupDescription(nameof(ImageContainerViewModel.ContainerType)));
                _groupedImageContainersViewSource.GroupDescriptions.Add(
                    new PropertyGroupDescription(nameof(ImageContainerViewModel.Year)));
                _groupedImageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.ContainerType), ListSortDirection.Ascending));
                _groupedImageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

                _groupedImageContainersViewSource.IsLiveFilteringRequested = true;
                _groupedImageContainersViewSource.IsLiveGroupingRequested = true;
                _groupedImageContainersViewSource.IsLiveSortingRequested = true;

                _albumImageContainersViewSource.Source = ViewModel.AlbumImageContainers;
                _albumImageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.Year), ListSortDirection.Descending));
                _albumImageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

                _albumImageContainersViewSource.IsLiveFilteringRequested = true;
                _albumImageContainersViewSource.IsLiveGroupingRequested = true;
                _albumImageContainersViewSource.IsLiveSortingRequested = true;

                //ImagesListScrollViewer = ImagesList.FindVisualChildren<ScrollViewer>().First();

                this.BindCommand(ViewModel,
                        model => model.AddFolder,
                        window => window.AddFolder)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        model => model.AddNewAlbum,
                        window => window.NewAlbum)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        model => model.OpenFolderManager,
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

//                ImagesList.Events().SelectionChanged
//                    .Subscribe(ea =>
//                    {
//                        ViewModel.ChangeSelectedImages(
//                            ea.AddedItems.Cast<ImageViewModel>(),
//                            ea.RemovedItems.Cast<ImageViewModel>());
//                    }).DisposeWith(d);

//                ViewModel.UnselectImage
//                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
//                    .Subscribe(imageViewModels =>
//                    {
//                        foreach (var imageViewModel in imageViewModels)
//                            ImagesList.SelectedItems.Remove(imageViewModel);
//                    })
//                    .DisposeWith(d);

                TrayImagesList.Events().SelectionChanged.Subscribe(ea =>
                {
                    using (ViewModel.SelectedTrayImages.SuspendNotifications())
                    {
                        ViewModel.SelectedTrayImages.RemoveMany(ea.RemovedItems.Cast<TrayImageViewModel>());
                        ViewModel.SelectedTrayImages.AddRange(ea.AddedItems.Cast<TrayImageViewModel>());
                    }
                }).DisposeWith(d);

//                ViewModel.UnselectTrayImage
//                    .ObserveOn(_schedulerProvider.MainThreadScheduler)
//                    .Subscribe(trayImageViewModels =>
//                    {
//                        foreach (var trayImageViewModel in trayImageViewModels)
//                            TrayImagesList.SelectedItems.Remove(trayImageViewModel);
//                    })
//                    .DisposeWith(d);

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

            ViewModel.AddNewAlbumWithImages.Execute(imageViewModels)
                .Subscribe();
        }
    }
}