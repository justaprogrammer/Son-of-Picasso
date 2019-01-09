using System;
using System.Windows.Media.Imaging;

namespace SonOfPicasso.Windows.ViewModels.DesignTimeData
{
    public class ImageViewModelSampleData : IImageViewModel
    {
        public string File { get; set; }
        public IObservable<BitmapImage> Image { get; set; }
    }
}