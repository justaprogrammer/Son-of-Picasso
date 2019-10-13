using System;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

            var imageCollectionViewSource = (CollectionViewSource) FindResource("ImagesCollectionViewSource");
            var imageContainersViewSource = (CollectionViewSource) FindResource("ImageContainersViewSource");
            
            this.WhenActivated(d =>
            {
                imageCollectionViewSource.Source = ViewModel.ImageViewModels;

                var propertyGroupDescription = new PropertyGroupDescription("ImageContainerViewModel");
                d(ViewModel.ImageContainerViewModels.ToObservableChangeSet()
                    .Subscribe(changeSets =>
                    {
                        foreach (var chagetSet in changeSets)
                        {
                            switch (chagetSet.Reason)
                            {
                                case ListChangeReason.AddRange:
                                    propertyGroupDescription.GroupNames.AddRange(chagetSet.Range.ToArray());
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }));

                imageCollectionViewSource.GroupDescriptions.Add(propertyGroupDescription);
                imageCollectionViewSource.SortDescriptions.Add(new SortDescription("ContainerType", ListSortDirection.Ascending));
                imageCollectionViewSource.SortDescriptions.Add(new SortDescription("ContainerYear", ListSortDirection.Descending));
                imageCollectionViewSource.SortDescriptions.Add(new SortDescription("ContainerDate", ListSortDirection.Descending));

                imageContainersViewSource.Source = ViewModel.ImageContainerViewModels;
                imageContainersViewSource.GroupDescriptions.Add(new PropertyGroupDescription("ContainerType"));
                imageContainersViewSource.GroupDescriptions.Add(new PropertyGroupDescription("Year"));
                imageContainersViewSource.SortDescriptions.Add(new SortDescription("ContainerType", ListSortDirection.Ascending));
                imageContainersViewSource.SortDescriptions.Add(new SortDescription("Year", ListSortDirection.Descending));
                imageContainersViewSource.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

                d(this.BindCommand(ViewModel, 
                    model => model.AddFolder,
                    window => window.AddFolder));

                d(this.BindCommand(ViewModel,
                    model => model.NewAlbum,
                    window => window.NewAlbum));

                d(ViewModel.NewAlbumInteraction.RegisterHandler(context =>
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
                }));

                d(ViewModel.AddFolderInteraction.RegisterHandler(context =>
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
                }));
            });
        }
    }
}