using System;
using System.Collections.Generic;
using System.Reactive;
using SonOfPicasso.Core.Models;

namespace SonOfPicasso.Core.Interfaces
{
    public interface ISharedCache
    {
        IObservable<UserSettings> GetUserSettings();
        IObservable<Unit> SetUserSettings(UserSettings userSettings);

        IObservable<string[]> GetFolderList();
        IObservable<Unit> SetFolderList(string[] paths);

        IObservable<ImageFolder> GetFolder(string path);
        IObservable<Unit> SetFolder(ImageFolder imageFolder);

        IObservable<Unit> Clear();
    }
}