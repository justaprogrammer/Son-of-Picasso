using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageFolderViewControl.xaml
    /// </summary>
    public partial class ImageFolderViewControl : ReactiveUserControl<ImageFolderViewModel>
    {
        public ImageFolderViewControl()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        model => model.Path,
                        window => window.FolderName.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
