using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData;
using SonOfPicasso.Core.Model;

namespace SonOfPicasso.Core.Interfaces
{
    public interface IImageContainerWatcherService
    {
        IObservable<string> FileDiscovered { get; }
        IObservable<string> FileDeleted { get; }
        IObservable<(string oldFullPath, string fullPath)> FileRenamed { get; }
        void Start(IObservableCache<ImageRef, string> imageRefCache, IList<string> paths);
        void Stop();
    }
}