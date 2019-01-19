using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using ReactiveUI;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Core.Scheduling;
using SonOfPicasso.UI.DependencyInjection;
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

        public void Initialize(Image image)
        {
            this.Image = image;

            var imageFromPath = _imageLoadingService.LoadImageFromPath(image.Path);

            _bitmap = imageFromPath
                .Select(bitmap => bitmap.ToNative())
                .ObserveOn(_schedulerProvider.MainThreadScheduler)
                .ToProperty(this, x => x.Bitmap);
        }

        private Image _image;

        public Image Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }

        private ObservableAsPropertyHelper<BitmapSource> _bitmap;

        public BitmapSource Bitmap => _bitmap.Value;
    }
}