using System;
using System.Collections.Generic;
using System.Reactive;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IDataCache
    {
        IObservable<UserSettings> GetUserSettings();
        IObservable<Unit> SetUserSettings(UserSettings userSettings);

        IObservable<string[]> GetFolderList();
        IObservable<Unit> SetFolderList(string[] paths);

        IObservable<ImageFolderModel> GetFolder(string path);
        IObservable<Unit> SetFolder(ImageFolderModel imageFolder);
        IObservable<Unit> DeleteFolder(string path);

        IObservable<ImageModel> GetImage(string path);
        IObservable<Unit> SetImage(ImageModel image);
        IObservable<Unit> DeleteImage(string path);

        IObservable<Unit> Clear();
    }
}