using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Nito.Mvvm;
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


            Bitmap = NotifyTask.Create(taskCompletionSource.Task);
        }

        private Image _image;

        public Image Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }

        private NotifyTask<WeakReference<IBitmap>> _bitmap;

        public NotifyTask<WeakReference<IBitmap>> Bitmap
        {
            get => _bitmap;
            set => this.RaiseAndSetIfChanged(ref _bitmap, value);
        }
    }
}