using System;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;
using Splat;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageViewModel : ViewModelBase
    {
        private readonly ObservableAsPropertyHelper<BitmapSource> _imageOaph;

        public ImageViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider,
            ImageRef imageRef, ImageContainerViewModel imageContainerViewModel)
        {
            var imageLoadingService1 =
                imageLoadingService ?? throw new ArgumentNullException(nameof(imageLoadingService));
            var schedulerProvider1 = schedulerProvider ?? throw new ArgumentNullException(nameof(schedulerProvider));
            ImageRef = imageRef ?? throw new ArgumentNullException(nameof(imageRef));
            ImageContainerViewModel = imageContainerViewModel ??
                                      throw new ArgumentNullException(nameof(imageContainerViewModel));

            _imageOaph = imageLoadingService1
                .LoadImageFromPath(Path)
                .Select(bitmap => bitmap.ToNative())
                .ObserveOn(schedulerProvider1.MainThreadScheduler)
                .ToProperty(this, model => model.Image, deferSubscription: true);
        }

        public ImageRef ImageRef { get; }
        public ImageContainerViewModel ImageContainerViewModel { get; }
        public string ImageRefId => ImageRef.Key;
        public DateTime ExifDate => ImageRef.ExifDate;
        public int ImageId => ImageRef.Id;
        public string Path => ImageRef.ImagePath;
        public string ContainerKey => ImageContainerViewModel.ContainerKey;
        public ImageContainerTypeEnum ContainerType => ImageContainerViewModel.ContainerType;
        public int ContainerYear => ImageContainerViewModel.Year;
        public DateTime ContainerDate => ImageContainerViewModel.Date;
        public BitmapSource Image => _imageOaph.Value;
    }
}