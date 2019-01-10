using System;
using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewFor<IApplicationViewModel>
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystem _fileSystem;

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(IApplicationViewModel), typeof(MainWindow), new PropertyMetadata(null));

        public MainWindow(IEnvironmentService environmentService, IFileSystem fileSystem)
        {
            _environmentService = environmentService;
            _fileSystem = fileSystem;

            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.Bind(ViewModel,
                        model => model.PathToImages,
                        window => window.PathToImages.Text,
                        PathToImages.Events().KeyUp)
                    .DisposeWith(disposable);

                this.BindCommand(ViewModel,
                        model => model.BrowseToDatabase,
                        window => window.BrowseToDatabaseButton)
                    .DisposeWith(disposable);

                ViewModel.BrowseToDatabase.IsExecuting
                    .Where(b => b)
                    .Subscribe(b => DoBrowseToImages(ViewModel.PathToImages))
                    .DisposeWith(disposable);
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

        private void DoBrowseToImages(string pathToDatabase)
        {
            var dialog = new FolderBrowserDialog();

            if(pathToDatabase == null)
            {
                dialog.SelectedPath = _environmentService.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else if (_fileSystem.Directory.Exists(pathToDatabase))
            {
                dialog.SelectedPath = pathToDatabase;
            }
            else
            {
                var fromDirectoryName = _fileSystem.DirectoryInfo.FromDirectoryName(pathToDatabase);
                while (fromDirectoryName.Parent != null)
                {
                    fromDirectoryName = fromDirectoryName.Parent;
                    if (fromDirectoryName.Exists)
                    {
                        dialog.SelectedPath = fromDirectoryName.FullName;
                        break;
                    }
                }
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.PathToImages = dialog.SelectedPath;
            }
        }
    }
}
