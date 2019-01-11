using System;
using System.Reactive;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Interfaces
{
    public interface ISharedCache
    {
        IObservable<UserSettings> GetUserSettings();
        IObservable<Unit> SetUserSettings(UserSettings userSettings);
        IObservable<ImageFolderDictionary> GetImageFolders();
        IObservable<Unit> SetImageFolders(ImageFolderDictionary imageFolders);
    }
}