using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageContainerViewControl.xaml
    /// </summary>
    public partial class ImageContainerTreeItemView : ReactiveUserControl<ImageContainerViewModel>
    {
        public ImageContainerTreeItemView()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        model => model.Name,
                        window => window.FolderName.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
