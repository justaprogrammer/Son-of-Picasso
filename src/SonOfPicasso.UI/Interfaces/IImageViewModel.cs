using System;
using System.Windows.Media.Imaging;
using Nito.Mvvm;
using SonOfPicasso.Core.Models;
using SonOfPicasso.UI.ViewModels;
using Splat;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageViewModel
    {
        void Initialize(Image image);
        Image Image { get; }
        NotifyTask<WeakReference<IBitmap>> Bitmap { get; set; }
    }
}