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
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ISchedulerProvider _schedulerProvider;
        private ObservableAsPropertyHelper<BitmapSource> _imageOaph;

        public ImageViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider)
        {
            _imageLoadingService = imageLoadingService;
            _schedulerProvider = schedulerProvider;
        }

        public ImageRef ImageRef { get; private set; }
        public ImageContainerViewModel ImageContainerViewModel { get; set; }
        public string ImageRefId => ImageRef.Key;
        public DateTime ExifDate => ImageRef.ExifDate;
        public int ImageId => ImageRef.Id;
        public string Path => ImageRef.ImagePath;
        public string ContainerId => ImageContainerViewModel.ContainerId;
        public ImageContainerTypeEnum ContainerType => ImageContainerViewModel.ContainerType;
        public int ContainerYear => ImageContainerViewModel.Year;
        public DateTime ContainerDate => ImageContainerViewModel.Date;

        public void Initialize(ImageRef imageRef, ImageContainerViewModel imageContainerViewModel)
        {
            ImageRef = imageRef ?? throw new ArgumentNullException(nameof(imageRef));
            ImageContainerViewModel = imageContainerViewModel ?? throw new ArgumentNullException(nameof(imageContainerViewModel));
            
            _imageOaph = _imageLoadingService
                .LoadImageFromPath(Path)
                .Select(bitmap => bitmap.ToNative())
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .ToProperty(this, model => model.Image, deferSubscription: true);
        }

        public BitmapSource Image => _imageOaph.Value;
    }
}