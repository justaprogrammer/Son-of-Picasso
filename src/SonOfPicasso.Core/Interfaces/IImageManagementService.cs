using System;
using System.Collections.Generic;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageManagementService
    {
        IObservable<ImageContainer> ScanFolder(string path);
        IObservable<ImageContainer> CreateAlbum(ICreateAlbum createAlbum);
        IObservable<ImageContainer> GetAllImageContainers();
        IObservable<ImageContainer> AddImagesToAlbum(int albumId, IEnumerable<int> imageIds);
    }
}