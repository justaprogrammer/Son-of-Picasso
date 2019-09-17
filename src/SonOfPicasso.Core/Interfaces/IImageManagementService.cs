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
        IObservable<Image> AddImagesToAlbum(int albumId, IList<int> imageIds);
        IObservable<Album[]> GetAlbums();
        IObservable<Image[]> GetImages();
        IObservable<Unit> RemoveImageFromAlbum(IList<int> albumImageIds);
        IObservable<Unit> DeleteImages(IList<int> imageIds);
        IObservable<Unit> DeleteAlbums(IList<int> albumIds);
    }
}