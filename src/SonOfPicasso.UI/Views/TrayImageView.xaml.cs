using System;
using ReactiveUI;
using SonOfPicasso.UI.ViewModels;
using Splat;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for ImageDetailView.xaml
    /// </summary>
    public partial class TrayImageView : ReactiveUserControl<TrayImageViewModel>
    {
        public TrayImageView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(ViewModel.Image.GetImage()
                    .Subscribe(bitmap =>
                    {
                        var imageBitmapSource = bitmap.ToNative();
                        ImageBitmap.Source = imageBitmapSource;
                    }));
            });
        }
    }
}
