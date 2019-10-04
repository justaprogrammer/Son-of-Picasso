using System;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;
using Splat;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageView.xaml
    /// </summary>
    public partial class ImageView : ReactiveUserControl<ImageViewModel>
    {
        public ImageView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                ImageLabel.Content = ViewModel.ImageRef.ImagePath;

                d(ViewModel.GetImage()
                    .Subscribe(bitmap =>
                    {
                        var imageBitmapSource = bitmap.ToNative();
                        ImageBitmap.Source = imageBitmapSource;
                    }));
            });
        }
    }
}
