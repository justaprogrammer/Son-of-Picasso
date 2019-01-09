using System;
using System.Windows.Media.Imaging;
using SonOfPicasso.Core;
using SonOfPicasso.Core.Logging;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Windows.ViewModels
{
    public class ImageViewModel : ReactiveObject, IImageViewModel
    {
        public ImageViewModel(ImageService imageService, string file)
            : this(imageService, file, new SchedulerProvider())
        {
        }

        public ImageViewModel(ImageService imageService, string file, ISchedulerProvider scheduler)
        {
            ImageService = imageService;
            File = file;

            Image = ImageService.LoadImageAsync(file);

            Log.Debug("Created");
        }

        private static ILogger Log { get; } = LogManager.ForContext<ImageViewModel>();

        public IObservable<BitmapImage> Image { get; }

        public string File { get; }

        protected ImageService ImageService { get; }
    }
}