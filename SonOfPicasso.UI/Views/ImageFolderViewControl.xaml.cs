using System.IO.Abstractions;
using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageFolderViewControl.xaml
    /// </summary>
    public partial class ImageFolderViewControl : ReactiveUserControl<IImageFolderViewModel>
    {
        public ImageFolderViewControl()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        model => model.ImageFolder.Path,
                        window => window.FolderName.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
