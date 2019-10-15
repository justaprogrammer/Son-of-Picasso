using System;
using System.Reactive.Disposables;
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
                ViewModel.Image.GetImage()
                    .Subscribe(bitmap =>
                    {
                        var imageBitmapSource = bitmap.ToNative();
                        ImageBitmap.Source = imageBitmapSource;
                    }).DisposeWith(d);

                this.OneWayBind(ViewModel,
                    model => model.Pinned,
                    view => view.ImageOverlay.Visibility);
            });
        }
    }
}
