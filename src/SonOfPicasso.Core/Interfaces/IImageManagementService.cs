using System;
using System.Collections.Generic;
using System.Reactive;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageManagementService
    {
        IObservable<IImageContainer> ScanFolder(string path);
        IObservable<IImageContainer> CreateAlbum(ICreateAlbum createAlbum);
        IObservable<IImageContainer> GetAllImageContainers();
        IObservable<IImageContainer> AddImagesToAlbum(int albumId, IEnumerable<int> imageIds);
        IObservable<IImageContainer> AddImage(string path);
        IObservable<IImageContainer> DeleteImage(string path);
        IObservable<IImageContainer> UpdateImage(string path);
        IObservable<IImageContainer> RenameImage(string oldPath, string newPath);
        IObservable<Unit> DeleteAlbum(int albumId);
    }
}