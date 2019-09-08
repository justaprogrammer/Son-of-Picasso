using System;
using System.Collections.Generic;
using System.Reactive;
using SonOfPicasso.Core.Models;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageManagementService
    {
        IObservable<Image[]> ScanFolder(string path);
        IObservable<Album> CreateAlbum(string name);
        IObservable<Image> AddImagesToAlbum(string albumName, IEnumerable<string> imagePaths);
    }
}