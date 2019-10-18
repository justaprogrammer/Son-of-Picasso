using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Windows.Dialogs
{
    /// <summary>
    ///     Interaction logic for AddAlbumWindow.xaml
    /// </summary>
    public partial class FolderManagementWindow : ReactiveWindow<FolderManagementViewModel>
    {
        public FolderManagementWindow(ISchedulerProvider schedulerProvider)
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                FoldersListView.ItemsSource = ViewModel.Folders;
                
                this.BindCommand(ViewModel,
                        model => model.Continue,
                        window => window.OkButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel,
                        model => model.Cancel,
                        window => window.CancelButton)
                    .DisposeWith(d);

                ViewModel.CancelInteraction.RegisterHandler(context =>
                    {
                        return Observable.Defer(() =>
                        {
                            context.SetOutput(Unit.Default);
                            DialogResult = false;
                            Close();
                            return Observable.Return(Unit.Default);
                        }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                    })
                    .DisposeWith(d);

                ViewModel.ContinueInteraction.RegisterHandler(context =>
                    {
                        return Observable.Defer(() =>
                        {
                            context.SetOutput(Unit.Default);
                            DialogResult = true;
                            Close();
                            return Observable.Return(Unit.Default);
                        }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                    })
                    .DisposeWith(d);
            });
        }
    }
}