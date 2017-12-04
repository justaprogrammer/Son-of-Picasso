using System;
using System.Reactive.Concurrency;
using System.Windows.Media.Imaging;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using PicasaReboot.Core.Scheduling;
using ReactiveUI;
using Serilog;

namespace PicasaReboot.Windows.ViewModels
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