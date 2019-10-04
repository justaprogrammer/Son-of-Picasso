using System;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.ViewModels.Abstract;
using Splat;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageRefViewModel : ViewModelBase
    {
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageRefViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider,
            ViewModelActivator activator) : base(activator)
        {
            _imageLoadingService = imageLoadingService;
            _schedulerProvider = schedulerProvider;
        }

        public ImageRef ImageRef { get; private set; }
        public int Id => ImageRef.ImageId;
        public string Path => ImageRef.ImagePath;

        public void Initialize(ImageRef imageRef)
        {
            ImageRef = imageRef ?? throw new ArgumentNullException(nameof(imageRef));
        }

        public IObservable<IBitmap> GetImage()
        {
            return _imageLoadingService.LoadImageFromPath(Path)
                .ObserveOn(_schedulerProvider.MainThreadScheduler);
        }
    }
}