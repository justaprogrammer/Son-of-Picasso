using System.IO.Abstractions;
using System.Windows.Media.Imaging;
using PicasaReboot.Core;
using ReactiveUI;
using Serilog;

namespace PicasaReboot.Windows.ViewModels
{
    public class ImageViewModel: ReactiveObject, IImageViewModel
    {
        private static ILogger Log { get; } = LogManager.ForContext<ImageViewModel>();

        protected ImageService ImageService { get; }

        private BitmapImage _image;

        public BitmapImage Image
        {
            get { return _image; }
            set { this.RaiseAndSetIfChanged(ref _image, value); }
        }

        private string _file;

        public string File
        {
            get { return _file; }
            set { this.RaiseAndSetIfChanged(ref _file, value); }
        }

        public ImageViewModel(ImageService imageService, string file)
        {
            ImageService = imageService;
            File = file;

            Image = ImageService.LoadImage(file);
            Log.Debug("Created");
        }
    }
}