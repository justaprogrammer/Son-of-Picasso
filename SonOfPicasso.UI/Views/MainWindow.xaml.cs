using System;
using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewFor<IApplicationViewModel>
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystem _fileSystem;
        private readonly ISchedulerProvider _schedulerProvider;

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(IApplicationViewModel), typeof(MainWindow), new PropertyMetadata(null));

        public MainWindow(ILogger<MainWindow> logger, IEnvironmentService environmentService, IFileSystem fileSystem, ISchedulerProvider schedulerProvider)
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
                        window => window.FoldersListView.ItemsSource);

                    this.OneWayBind(ViewModel, 
                        model => model.Images, 
                        window => window.ImagesListView.ItemsSource);
                });
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (IApplicationViewModel)value;
        }

        public IApplicationViewModel ViewModel
        {
            get => (IApplicationViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
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
                _logger.LogDebug("Adding Folder {0}", selectedPath);

                ViewModel.AddFolder.Execute(selectedPath).Subscribe();
            }
        }
    }
}
