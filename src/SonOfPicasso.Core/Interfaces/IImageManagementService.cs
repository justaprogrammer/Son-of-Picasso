using System;
using System.Reactive;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Services
{
    public interface IImageManagementService
    {
        IObservable<Unit> AddFolder(string path);
        IObservable<Unit> RemoveFolder(string path);
        IObservable<ImageFolderModel> GetAllImageFolders();
        IObservable<ImageModel> GetAllImages();
    }
}