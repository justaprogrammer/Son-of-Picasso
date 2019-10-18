using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels;
using SonOfPicasso.UI.Windows.Dialogs;
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
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private CollectionViewSource imageCollectionViewSource;
        private CollectionViewSource imageContainersViewSource;
        private CollectionViewSource albumImageContainersViewSource;

        public MainWindow(ILogger logger, IEnvironmentService environmentService, IFileSystem fileSystem,
            ISchedulerProvider schedulerProvider, Func<AddAlbumWindow> addAlbumWindowFactory,
            Func<AddAlbumViewModel> addAlbumViewModelFactory)
        {
            _logger = logger;
            _environmentService = environmentService;
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;
            _addAlbumWindowFactory = addAlbumWindowFactory;
            _addAlbumViewModelFactory = addAlbumViewModelFactory;

            InitializeComponent();

            imageCollectionViewSource = (CollectionViewSource) FindResource("ImagesCollectionViewSource");
            imageContainersViewSource = (CollectionViewSource) FindResource("ImageContainersViewSource");
            albumImageContainersViewSource = (CollectionViewSource) FindResource("AlbumImageContainersViewSource");

            this.WhenActivated(d =>
            {
                imageCollectionViewSource.Source = ViewModel.Images;

                var propertyGroupDescription = new PropertyGroupDescription(nameof(ImageViewModel.ImageContainerViewModel));

                imageCollectionViewSource.GroupDescriptions.Add(propertyGroupDescription);
                imageCollectionViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageViewModel.ContainerType),
                    ListSortDirection.Ascending));
                imageCollectionViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageViewModel.ContainerDate),
                    ListSortDirection.Descending));
                imageCollectionViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageViewModel.Date),
                    ListSortDirection.Ascending));
                
                imageContainersViewSource.Source = ViewModel.ImageContainers;
                imageContainersViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ImageContainerViewModel.ContainerType)));
                imageContainersViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ImageContainerViewModel.Year)));
                imageContainersViewSource.SortDescriptions.Add(new SortDescription(nameof(ImageContainerViewModel.ContainerType),
                    ListSortDirection.Ascending));
                imageContainersViewSource.SortDescriptions.Add(
                    new SortDescription(nameof(ImageContainerViewModel.Date), ListSortDirection.Descending));

                albumImageContainersViewSource.Source = ViewModel.AlbumImageContainers;
                albumImageContainersViewSource.SortDescriptions.Add(
                    new SortDescription("Year", ListSortDirection.Descending));
                albumImageContainersViewSource.SortDescriptions.Add(
                    new SortDescription("Date", ListSortDirection.Descending));

                this.BindCommand(ViewModel,
                        model => model.AddFolder,
                        window => window.AddFolder)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        model => model.NewAlbum,
                        window => window.NewAlbum)
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
                                .ToObservable()
                                .ToList()
                            ).SelectMany(observable => observable))
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                    model => model.ClearTrayItems,
                    window => window.ClearTrayItems,
                    observableSelectedTrayImageCount
                        .Select(selectedTrayCount =>
                        {
                            var allItems = selectedTrayCount == 0;

                            return (!allItems ? ViewModel.SelectedTrayImages : ViewModel.TrayImages)
                                .ToObservable()
                                .ToList()
                                .CombineLatest(Observable.Return(allItems), (list, b) => (list, b));
                        })
                        .SelectMany(observable => observable)).DisposeWith(d);

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
                        {
                            ImagesList.SelectedItems.Remove(imageViewModel);
                        }
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
                        {
                            TrayImagesList.SelectedItems.Remove(trayImageViewModel);
                        }
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
                        var messageBoxResult = MessageBox.Show("This will clear items in the tray. Are you sure?", "Confirmation", MessageBoxButton.YesNo);
                        context.SetOutput(messageBoxResult == MessageBoxResult.Yes);
                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(_schedulerProvider.MainThreadScheduler);
                }).DisposeWith(d);
            });
        }

        private void AlbumButton_AddImagesToAlbum_OnClick(object sender, RoutedEventArgs e)
        {
            var menuItem = (System.Windows.Controls.MenuItem)sender;
            var imageContainerViewModel = (ImageContainerViewModel) menuItem.DataContext;

            var viewModelSelectedTrayImages = ViewModel.SelectedTrayImages.Any() ? ViewModel.SelectedTrayImages : ViewModel.TrayImages;
            var imageViewModels = viewModelSelectedTrayImages.Select(model => model.Image).ToList();

            ViewModel.AddImagesToAlbum.Execute((imageViewModels.AsEnumerable(), imageContainerViewModel))
                .Subscribe();
        }

        private void AlbumButton_AddAlbum_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModelSelectedTrayImages = ViewModel.SelectedTrayImages.Any() ? ViewModel.SelectedTrayImages : ViewModel.TrayImages;
            var imageViewModels = viewModelSelectedTrayImages.Select(model => model.Image).ToList();

            ViewModel.NewAlbumWithImages.Execute(imageViewModels)
                .Subscribe();
        }
    }
}