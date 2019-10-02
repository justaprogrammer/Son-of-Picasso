using System;
using System.Reactive;
using SonOfPicasso.Core.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IDataCache
    {
        IObservable<UserSettings> GetUserSettings();
        IObservable<Unit> SetUserSettings(UserSettings userSettings);
        IObservable<Unit> Clear();
    }
}