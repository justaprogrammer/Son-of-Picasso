using System;
using System.Threading.Tasks;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
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

        public ImageViewModel(IImageLoadingService imageLoadingService)
        {
            _imageLoadingService = imageLoadingService;
        }

        public void Initialize(Image image)
        {
            this.Image = image;

            var taskCompletionSource = new TaskCompletionSource<WeakReference<IBitmap>>();

            _imageLoadingService.LoadImageFromPath(image.Path)
                .Subscribe(bitmap => taskCompletionSource.SetResult(new WeakReference<IBitmap>(bitmap)));
        }

        private Image _image;

        public Image Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }

        public IObservable<IBitmap> GetImage()
        {
            return _imageLoadingService.LoadImageFromPath(Image.Path);
        }
    }
}