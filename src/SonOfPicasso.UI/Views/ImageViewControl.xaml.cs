using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageViewControl.xaml
    /// </summary>
    public partial class ImageViewControl : ReactiveUserControl<IImageViewModel>
    {
        public ImageViewControl()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        model => model.Bitmap,
                        window => window.ImageBitmap.Source)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        model => model.Image.Path,
                        window => window.ImageLabel.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
