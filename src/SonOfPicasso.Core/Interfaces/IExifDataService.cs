using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IExifDataService
    {
        IObservable<ExifData> GetExifData(string path);
    }
}