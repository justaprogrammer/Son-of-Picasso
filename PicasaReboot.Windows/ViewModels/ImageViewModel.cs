using System.IO.Abstractions;
using System.Reactive.Concurrency;
using System.Windows.Media.Imaging;
using PicasaReboot.Core;
using PicasaReboot.Core.Logging;
using ReactiveUI;
using Serilog;

namespace PicasaReboot.Windows.ViewModels
{
    public class ImageViewModel : ReactiveObject, IImageViewModel
    {
        private string _file;

        private BitmapImage _image;

        public ImageViewModel(ImageService imageService, string file)
            : this(imageService, file, DefaultScheduler.Instance)
        {
        }

        public ImageViewModel(ImageService imageService, string file, IScheduler scheduler)
        {
            ImageService = imageService;
            File = file;

            Image = ImageService.LoadImage(file);
            Log.Debug("Created");
        }

        private static ILogger Log { get; } = LogManager.ForContext<ImageViewModel>();

        protected ImageService ImageService { get; }

        public BitmapImage Image
        {
            get { return _image; }
            set { this.RaiseAndSetIfChanged(ref _image, value); }
        }

        public string File
        {
            get { return _file; }
            set { this.RaiseAndSetIfChanged(ref _file, value); }
        }
    }
}