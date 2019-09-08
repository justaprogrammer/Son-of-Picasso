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
        IObservable<Image> AddImagesToAlbum(int albumId, IEnumerable<int> imageIds);
        IObservable<Album[]> GetAlbums();
        IObservable<Unit> DeleteAlbum(int id);
    }
}