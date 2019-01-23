using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageViewControl.xaml
    /// </summary>
    public partial class ImageViewControl : ReactiveUserControl<IImageViewModel>
    {
        public ImageViewControl(IImageViewModelBitmapConveter bitmapConveter)
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        model => model,
                        window => window.ImageBitmap.Source,
                        vmToViewConverterOverride: bitmapConveter)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        model => model.Image.Path,
                        window => window.ImageLabel.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}
