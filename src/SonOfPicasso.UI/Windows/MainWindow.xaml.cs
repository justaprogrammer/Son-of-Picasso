using System;
using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Forms;
using ReactiveUI;
using Serilog;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ReactiveWindow<IApplicationViewModel>
    {
        private readonly ILogger _logger;
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystem _fileSystem;
        private readonly ISchedulerProvider _schedulerProvider;

        public MainWindow(ILogger logger, IEnvironmentService environmentService, IFileSystem fileSystem, ISchedulerProvider schedulerProvider)
        {
            _logger = logger;
            _environmentService = environmentService;
            _fileSystem = fileSystem;
            _schedulerProvider = schedulerProvider;

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
    }
}
