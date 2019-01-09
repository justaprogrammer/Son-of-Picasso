using System;
using System.Windows.Media.Imaging;

namespace SonOfPicasso.Windows.ViewModels
{
    public interface IImageViewModel
    {
        string File { get; }

        IObservable<BitmapImage> Image { get; }
    }
}