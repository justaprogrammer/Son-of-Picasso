using System;
using SonOfPicasso.Core.Models;
using Splat;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageViewModel
    {
        void Initialize(ImageModel imageModel);
        ImageModel ImageModel { get; }
        IObservable<IBitmap> GetImage();
    }
}