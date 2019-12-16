using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using DynamicData;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.ViewModels.FolderRules;
using SonOfPicasso.UI.Views;
using SonOfPicasso.UI.Windows.Dialogs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;

namespace SonOfPicasso.UI.Windows
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ReactiveWindow<ApplicationViewModel>
    {
        public MainWindow(ILogger logger,
            IEnvironmentService environmentService,
            ISchedulerProvider schedulerProvider,
            Func<AddAlbumWindow> addAlbumWindowFactory,
            Func<AddAlbumViewModel> addAlbumViewModelFactory,
            Func<FolderManagementWindow> folderManagementWindowFactory,
            Func<ManageFolderRulesViewModel> folderManagementViewModelFactory)
        {
            InitializeComponent();

            var imageContainersViewSource = (CollectionViewSource) FindResource("ImageContainersViewSource");
            
            imageContainersViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageContainerViewModel.ContainerType), ListSortDirection.Ascending));
            imageContainersViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

            imageContainersViewSource.IsLiveFilteringRequested = true;
            imageContainersViewSource.IsLiveGroupingRequested = true;
            imageContainersViewSource.IsLiveSortingRequested = true;

            var groupedImageContainersViewSource = (CollectionViewSource) FindResource("GroupedImageContainersViewSource");
            
            groupedImageContainersViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ImageContainerViewModel.ContainerType)));
            groupedImageContainersViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ImageContainerViewModel.Year)));

            groupedImageContainersViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageContainerViewModel.ContainerType), ListSortDirection.Ascending));
            groupedImageContainersViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

            groupedImageContainersViewSource.IsLiveFilteringRequested = true;
            groupedImageContainersViewSource.IsLiveGroupingRequested = true;
            groupedImageContainersViewSource.IsLiveSortingRequested = true;

            var albumImageContainersViewSource = (CollectionViewSource) FindResource("AlbumImageContainersViewSource");

            albumImageContainersViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageContainerViewModel.Year), ListSortDirection.Descending));
            albumImageContainersViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

            albumImageContainersViewSource.IsLiveFilteringRequested = true;
            albumImageContainersViewSource.IsLiveGroupingRequested = true;
            albumImageContainersViewSource.IsLiveSortingRequested = true;

            ImageZoomSlider.Events().MouseDoubleClick.Subscribe(args => { ImageZoomSlider.Value = 100; });

            this.WhenActivated(d =>
            {
                imageContainersViewSource.Source = ViewModel.ImageContainers;
                groupedImageContainersViewSource.Source = ViewModel.ImageContainers;
                albumImageContainersViewSource.Source = ViewModel.AlbumImageContainers;

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

                ViewModel.ClearTrayItems.Subscribe(list =>
                {
                    if (list.Any())
                        ImageContainerListView.ClearSelectedItems(list);
                });

                Observable
                    .FromEventPattern<ImageContainerListView.ImageSelectionChangedEventHandler,
                        ImageSelectionChangedEventArgs>(
                        handler => ImageContainerListView.ImageSelectionChanged += handler,
                        handler => ImageContainerListView.ImageSelectionChanged -= handler)
                    .Select(pattern => pattern.EventArgs)
                    .Subscribe(eventArgs =>
                    {
                        ViewModel.ChangeSelectedImages(eventArgs.AddedItems, eventArgs.RemovedItems);
                    }).DisposeWith(d);

                TrayImagesList.Events().SelectionChanged.Subscribe(ea =>
                {
                    using (ViewModel.SelectedTrayImages.SuspendNotifications())
                    {
                        ViewModel.SelectedTrayImages.RemoveMany(ea.RemovedItems.Cast<TrayImageViewModel>());
                        ViewModel.SelectedTrayImages.AddRange(ea.AddedItems.Cast<TrayImageViewModel>());
                    }
                }).DisposeWith(d);

                this.OneWayBind(ViewModel,
                    model => model.TrayImages,
                    window => window.TrayImagesList.ItemsSource).DisposeWith(d);

                ViewModel.NewAlbumInteraction.RegisterHandler(context =>
                {
                    return Observable.Defer(() =>
                    {
                        var addAlbumWindow = addAlbumWindowFactory();
                        var addAlbumViewModel = addAlbumViewModelFactory();

                        addAlbumWindow.ViewModel = addAlbumViewModel;

                        AddAlbumViewModel result = null;
                        if (addAlbumWindow.ShowDialog() == true) result = addAlbumWindow.ViewModel;

                        context.SetOutput(result);
                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                }).DisposeWith(d);

                ViewModel.AddFolderInteraction.RegisterHandler(context =>
                {
                    return Observable.Defer(() =>
                    {
                        var dialog = new FolderBrowserDialog
                        {
                            SelectedPath = environmentService.GetFolderPath(Environment.SpecialFolder.MyPictures)
                        };

                        string result = null;
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) result = dialog.SelectedPath;

                        context.SetOutput(result);
                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(schedulerProvider.MainThreadScheduler);
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
                    }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                }).DisposeWith(d);

                ViewModel.FolderManagerInteraction.RegisterHandler(context =>
                    {
                        return Observable.Defer(async () =>
                        {
                            var folderManagementWindow = folderManagementWindowFactory();
                            var folderManagementViewModel = folderManagementViewModelFactory();

                            using (logger.BeginTimedOperation("Initializing FolderManagementViewModel",
                                level: LogEventLevel.Debug))
                            {
                                await folderManagementViewModel.Initialize();
                            }

                            folderManagementWindow.ViewModel = folderManagementViewModel;

                            ManageFolderRulesViewModel result = null;
                            if (folderManagementWindow.ShowDialog() == true) result = folderManagementWindow.ViewModel;

                            context.SetOutput(result);
                            return Observable.Return(Unit.Default);
                        }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                    }).DisposeWith(d);

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
                    }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                }).DisposeWith(d);
            });
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

            ViewModel.AddNewAlbumWithImages.Execute(imageViewModels)
                .Subscribe();
        }
    }
}