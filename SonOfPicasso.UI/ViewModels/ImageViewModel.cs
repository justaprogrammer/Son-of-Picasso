using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Interfaces;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageViewModel : ReactiveObject, IImageViewModel
    {
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider)
        {
            _imageLoadingService = imageLoadingService;
            _schedulerProvider = schedulerProvider;
        }

        public Image Image { get; private set; }

        public void Initialize(Image image)
        {
            this.Image = image;

            _imageLoadingService.LoadImageFromPath(image.Path)
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
        }
    }
}