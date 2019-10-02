using System;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.ViewModels.Abstract;
using Splat;

namespace SonOfPicasso.UI.ViewModels
{
    public class ImageViewModel : ViewModelBase
    {
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ISchedulerProvider _schedulerProvider;

        public string Path => _imageModel.Path;

        public ImageViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider, ViewModelActivator activator):base(activator)
        {
            _imageLoadingService = imageLoadingService;
            _schedulerProvider = schedulerProvider;
        }

        public void Initialize(Image imageModel)
        {
            _imageModel = imageModel ?? throw new ArgumentNullException(nameof(imageModel));
        }

        private Image _imageModel;

        public IObservable<IBitmap> GetImage()
        {
            return _imageLoadingService.LoadImageFromPath(Path)
                .ObserveOn(_schedulerProvider.MainThreadScheduler);
        }
    }
}