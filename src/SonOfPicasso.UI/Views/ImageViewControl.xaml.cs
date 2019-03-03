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
                ImageLabel.Content = ViewModel.Path;

                ViewModel.GetImage()
                    .Subscribe(bitmap => ImageBitmap.Source = bitmap.ToNative())
                    .DisposeWith(disposable);
            });
        }
    }
}
