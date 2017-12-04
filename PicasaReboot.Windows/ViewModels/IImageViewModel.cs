using System;
using System.Windows.Media.Imaging;

namespace PicasaReboot.Windows.ViewModels
{
    public interface IImageViewModel
    {
        string File { get; }

        IObservable<BitmapImage> Image { get; }
    }
}