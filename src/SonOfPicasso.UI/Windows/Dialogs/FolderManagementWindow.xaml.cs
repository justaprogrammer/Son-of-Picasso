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
    public partial class FolderManagementWindow : ReactiveWindow<FolderManagementViewModel>
    {
        public FolderManagementWindow(ISchedulerProvider schedulerProvider)
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
              

                d(this.BindCommand(ViewModel, 
                    model => model.Continue, 
                    window => window.OkButton));

                d(this.BindCommand(ViewModel, 
                    model => model.Cancel, 
                    window => window.CancelButton));

                d(this.ViewModel.CancelInteraction.RegisterHandler(context =>
                {
                    return Observable.Defer<Unit>(() =>
                    {
                        context.SetOutput(Unit.Default);
                        DialogResult = false;
                        Close();
                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                }));

                d(this.ViewModel.ContinueInteraction.RegisterHandler(context =>
                {
                    return Observable.Defer(() =>
                    {
                        context.SetOutput(Unit.Default);
                        DialogResult = true;
                        Close();
                        return Observable.Return(Unit.Default);
                    }).SubscribeOn(schedulerProvider.MainThreadScheduler);
                }));
            });
        }
    }
}