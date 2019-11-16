using System;
using System.Reactive;
using DynamicData;
using SonOfPicasso.Core.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageContainerWatcherService
    {
        IObservable<Unit> Start(IObservableCache<ImageRef, string> imageRefCache);
    }
}