using System;
using System.IO.Abstractions;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageLocationService
    {
        IObservable<IFileInfo> GetImages(string path);
    }
}