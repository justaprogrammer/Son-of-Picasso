using System;
using System.Reactive;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageManagementService
    {
        IObservable<(ImageFolderModel, ImageModel[])> AddFolder(string path);
        IObservable<Unit> RemoveFolder(string path);
        IObservable<ImageFolderModel> GetAllImageFolders();
        IObservable<ImageModel> GetAllImages();
    }
}