using System;
using System.Reactive;
using DynamicData;
using SonOfPicasso.Core.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageContainerWatcherService
    {
        IObservable<string> FileDiscovered { get; }
        IObservable<string> FileDeleted { get; }
        IObservable<(string oldFullPath, string fullPath)> FileRenamed { get; }
        IObservable<Unit> Start(IObservableCache<ImageRef, string> imageRefCache);
        void Stop();
    }
}