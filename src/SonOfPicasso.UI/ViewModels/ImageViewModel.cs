using System;
using System.Reactive.Linq;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.Data.Model;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Views;
using Splat;

namespace SonOfPicasso.UI.ViewModels
{
    [ViewModelView(typeof(ImageViewControl))]
    public class ImageViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ISchedulerProvider _schedulerProvider;

        public string Path => _imageModel.Path;

        public ImageViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider, ViewModelActivator activator)
        {
            _imageLoadingService = imageLoadingService;
            _schedulerProvider = schedulerProvider;
            Activator = activator;
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

        public ViewModelActivator Activator { get; }
    }
}