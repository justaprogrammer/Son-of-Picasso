using System;
using System.Windows.Media.Imaging;

namespace PicasaReboot.Windows.ViewModels.DesignTimeData
{
    public class ImageViewModelSampleData : IImageViewModel
    {
        public string File { get; set; }
        public IObservable<BitmapImage> Image { get; set; }
    }
}