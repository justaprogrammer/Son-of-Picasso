using System;
using System.Windows;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using Serilog;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Windows.Dialogs
{
    /// <summary>
    ///     Interaction logic for AddAlbumWindow.xaml
    /// </summary>
    public partial class AddAlbumWindow : ReactiveWindow<AddAlbumViewModel>
    {
        private readonly ILogger _logger;

        public AddAlbumWindow(ILogger logger)
        {
            _logger = logger;

            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel,
                    viewModel => viewModel.AlbumName,
                    view => view.TextAlbumName.Text));

                d(this.OneWayBind(ViewModel, model => model.DisplayAlbumNameError,
                    window => window.TextAlbumName.Style,
                    b => b ? Styles.TextBoxError : default));

                d(this.BindValidation(ViewModel,
                    vm => vm.AlbumNameRule,
                    view => view.LabelAlbumNameError.Content));

                d(this.BindCommand(ViewModel, 
                    model => model.Continue, 
                    window => window.OkButton));

                d(ViewModel.Continue.Subscribe(unit =>
                {
                    DialogResult = true;
                    Close();
                }));

                d(ViewModel.Cancel.Subscribe(unit =>
                {
                    DialogResult = false;
                    Close();
                }));
            });
        }
    }
}