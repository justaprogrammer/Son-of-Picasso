using System;
using System.Collections.Generic;
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
    }
}