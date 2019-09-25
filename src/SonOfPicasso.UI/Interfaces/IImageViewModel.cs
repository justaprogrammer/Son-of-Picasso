using System;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Data.Model;
using Splat;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageViewModel
    {
        void Initialize(Image image);
        IObservable<IBitmap> GetImage();
        string Path { get; }
    }
}