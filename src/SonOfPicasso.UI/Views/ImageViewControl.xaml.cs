using System;
using System.Reactive.Disposables;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;
using Splat;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageViewControl.xaml
    /// </summary>
    public partial class ImageViewControl : ReactiveUserControl<ImageViewModel>
    {
        public ImageViewControl()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                ImageLabel.Content = ViewModel.Path;

                ViewModel.GetImage()
                    .Subscribe(bitmap =>
                    {
                        var imageBitmapSource = bitmap.ToNative();
                        ImageBitmap.Source = imageBitmapSource;
                    })
                    .DisposeWith(disposable);
            });
        }
    }
}
