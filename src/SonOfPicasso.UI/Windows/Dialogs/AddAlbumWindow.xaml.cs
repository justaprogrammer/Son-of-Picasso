using System;
using System.Windows;
using ReactiveUI;
using Serilog;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for AddAlbumWindow.xaml
    /// </summary>
    public partial class AddAlbumWindow : ReactiveWindow<IAddAlbumViewModel>
    {
        private readonly ILogger _logger;

        public AddAlbumWindow(ILogger logger)
        {
            _logger = logger;

            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.Bind(ViewModel,
                    viewModel => viewModel.AlbumName,
                    view => view.TextAlbumName.Text);
            });
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Continue.Execute().Subscribe();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
