using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.Injection;
using SonOfPicasso.UI.Interfaces;
using SonOfPicasso.UI.Views;
using Splat;

namespace SonOfPicasso.UI.ViewModels
{
    [ViewModelView(typeof(ImageViewControl))]
    public class ImageViewModel : ReactiveObject, IImageViewModel
    {
        private readonly IImageLoadingService _imageLoadingService;
        private readonly ISchedulerProvider _schedulerProvider;

        public ImageViewModel(IImageLoadingService imageLoadingService, ISchedulerProvider schedulerProvider)
        {
            _imageLoadingService = imageLoadingService;
            _schedulerProvider = schedulerProvider;
        }

        public void Initialize(ImageModel imageModel)
        {
            this.ImageModel = imageModel;
        }

        private ImageModel _imageModel;

        public ImageModel ImageModel
        {
            get => _imageModel;
            set => this.RaiseAndSetIfChanged(ref _imageModel, value);
        }

        public IObservable<IBitmap> GetImage()
        {
            return _imageLoadingService.LoadImageFromPath(ImageModel.Path)
                .ObserveOn(_schedulerProvider.MainThreadScheduler);
        }
    }
}