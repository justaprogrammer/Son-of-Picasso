using System;
using SonOfPicasso.Core.Models;
using Splat;

namespace SonOfPicasso.UI.Interfaces
{
    public interface IImageViewModel
    {
        void Initialize();
        IObservable<IBitmap> GetImage();
        string Path { get; }
    }
}