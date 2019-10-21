using System.Windows;
using ReactiveUI;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    ///     Interaction logic for ManageFolderView.xaml
    /// </summary>
    public partial class ManageFolderView : ReactiveUserControl<FolderRuleViewModel>
    {
        public ManageFolderView(IImageProvider imageProvider)
        {
            ImageProvider = imageProvider;

            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel, model => model.Name, view => view.NameLabel.Content);

                this.OneWayBind(ViewModel, model => model.ManageFolderState, view => view.CancelImage.Visibility,
                    value => value == FolderRuleActionEnum.Remove ? Visibility.Visible : Visibility.Collapsed);

                this.OneWayBind(ViewModel, model => model.ManageFolderState, view => view.CheckmarkImage.Visibility,
                    value => value == FolderRuleActionEnum.Once ? Visibility.Visible : Visibility.Collapsed);

                this.OneWayBind(ViewModel, model => model.ManageFolderState, view => view.SynchronizeImage.Visibility,
                    value => value == FolderRuleActionEnum.Always ? Visibility.Visible : Visibility.Collapsed);
            });
        }

        public IImageProvider ImageProvider { get; }
    }
}