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
        public ImageViewControl(ISchedulerProvider schedulerProvider)
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        model => model.Image,
                        window => window.ImageLabel.Content,
                        image => image.Path)
                    .DisposeWith(disposable);

                this.ViewModel.GetImage()
                    .ObserveOn(schedulerProvider.MainThreadScheduler)
                    .Subscribe(bitmap =>
                    {
                        ImageBitmap.Source = bitmap.ToNative();
                    });
            });
        }
    }
}
