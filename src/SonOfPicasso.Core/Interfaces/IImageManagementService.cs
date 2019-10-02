using System;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageManagementService
    {
        IObservable<Image> ScanFolder(string path);
        IObservable<Album> CreateAlbum(ICreateAlbum createAlbum);
        IObservable<Image> GetImagesWithDirectoryAndExif();
        IObservable<Album> GetAllAlbumsWithAlbumImages();
        IObservable<ImageContainer> GetAllImageContainers();
    }
}