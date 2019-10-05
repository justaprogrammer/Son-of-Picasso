using System;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.ViewModels.Abstract;
using Splat;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageViewModel : ViewModelBase, IImageViewModel
    {
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider,
            ViewModelActivator activator) : base(activator)
        {
            _imageLoadingService = imageLoadingService;
            _schedulerProvider = schedulerProvider;
        }

        public IImageRowViewModel ImageRowViewModel { get; private set; }
        public ImageRef ImageRef { get; private set; }
        public int Id => ImageRef.ImageId;
        public string Path => ImageRef.ImagePath;

        public void Initialize(ImageRef imageRef, IImageRowViewModel imageRowViewModel)
        {
            ImageRef = imageRef ?? throw new ArgumentNullException(nameof(imageRef));
            ImageRowViewModel = imageRowViewModel ?? throw new ArgumentNullException(nameof(imageRowViewModel));
        }

        public IObservable<IBitmap> GetImage()
        {
            return _imageLoadingService.LoadImageFromPath(Path)
                .ObserveOn(_schedulerProvider.MainThreadScheduler);
        }
    }
}