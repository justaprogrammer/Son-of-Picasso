using System;
using System.IO.Abstractions;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageLocationService
    {
        IObservable<string> GetImages(string path);
    }
}