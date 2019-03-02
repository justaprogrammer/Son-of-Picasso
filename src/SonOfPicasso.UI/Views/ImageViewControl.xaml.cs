using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;
using Splat;

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
                        model => model.ImageModel,
                        window => window.ImageLabel.Content,
                        image => image.Path)
                    .DisposeWith(disposable);

                this.ViewModel.GetImage()
                    .Subscribe(bitmap =>
                    {
                        ImageBitmap.Source = bitmap.ToNative();
                    });
            });
        }
    }
}
