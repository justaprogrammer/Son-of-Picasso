using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels;
using Splat;

namespace SonOfPicasso.UI.Views
{
    /// <summary>
    /// Interaction logic for TrayImageView.xaml
    /// </summary>
    public partial class TrayImageView : ReactiveUserControl<TrayImageViewModel>
    {
        public TrayImageView(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider)
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel,
                    model => model.Pinned,
                    view => view.ImageOverlay.Visibility);

                imageLoadingService.LoadThumbnailFromPath(ViewModel.ImageViewModel.Path)
                    .ObserveOn(schedulerProvider.MainThreadScheduler)
                    .Subscribe(source => ImageBitmap.Source = source)
                    .DisposeWith(d);
            });
        }
    }
}
