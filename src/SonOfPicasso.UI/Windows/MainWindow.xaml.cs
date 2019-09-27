using System;
using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Forms;
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

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        model => model.ImageFolders,
                        window => window.FoldersListView.ItemsSource)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        model => model.Images,
                        window => window.ImagesListView.ItemsSource)
                    .DisposeWith(disposable);
            });
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                SelectedPath = _environmentService.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var selectedPath = dialog.SelectedPath;
                _logger.Debug("Adding Folder {0}", selectedPath);

                ViewModel.AddFolder.Execute(selectedPath).Subscribe();
            }
        }

        private void NewAlbum_Click(object sender, RoutedEventArgs e)
        {
            var addAlbumWindow = _addAlbumWindowFactory();
            var addAlbumViewModel = _addAlbumViewModelFactory();

            addAlbumWindow.ViewModel = addAlbumViewModel;
            addAlbumWindow.ShowDialog();
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}