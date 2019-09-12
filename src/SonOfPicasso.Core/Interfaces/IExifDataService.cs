using System;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public interface IExifDataService
    {
        IObservable<ExifData> GetExifData(string path);
    }
}