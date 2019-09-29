using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Windows.Dialogs
{
    /// <summary>
    ///     Interaction logic for AddAlbumWindow.xaml
    /// </summary>
    public partial class AddAlbumWindow : ReactiveWindow<AddAlbumViewModel>
    {
        public AddAlbumWindow(ISchedulerProvider schedulerProvider)
        {
            var schedulerProvider1 = schedulerProvider;

            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel,
                    viewModel => viewModel.AlbumName,
                    view => view.TextAlbumName.Text));

                d(this.Bind(ViewModel,
                    viewModel => viewModel.AlbumDate,
                    view => view.DateAlbumDate.SelectedDate));

                d(this.OneWayBind(ViewModel, model => model.DisplayAlbumNameError,
                    window => window.TextAlbumName.Style,
                    b => b ? Styles.TextBoxError : default));

                d(this.BindValidation(ViewModel,
                    vm => vm.AlbumNameRule,
                    view => view.LabelAlbumNameError.Content));

                d(this.BindCommand(ViewModel, 
                    model => model.Continue, 
                    window => window.OkButton));

                d(this.BindCommand(ViewModel, 
                    model => model.Cancel, 
                    window => window.CancelButton));

                d(this.ViewModel.CancelInteraction.RegisterHandler(context =>
                {
                    return Observable.Start(() =>
                    {
                        context.SetOutput(Unit.Default);
                        DialogResult = false;
                        Close();
                    }, schedulerProvider1.MainThreadScheduler);
                }));

                d(this.ViewModel.ContinueInteraction.RegisterHandler(context =>
                {
                    return Observable.Start(() =>
                    {
                        context.SetOutput(Unit.Default);
                        DialogResult = true;
                        Close();
                    }, schedulerProvider1.MainThreadScheduler);
                }));
            });
        }
    }
}