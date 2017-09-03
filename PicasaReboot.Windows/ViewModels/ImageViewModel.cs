using System.IO.Abstractions;
using PicasaReboot.Core;
using ReactiveUI;

namespace PicasaReboot.Windows.ViewModels
{
    public class ImageViewModel: ReactiveObject, IImageViewModel
    {
        protected ImageService ImageService { get; }

        public string File { get; }

        public ImageViewModel(ImageService imageService, string file)
        {
            ImageService = imageService;
            File = file;
        }

        public void Initialize()
        {
        }
    }
}