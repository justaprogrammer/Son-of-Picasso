using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class FolderWatcherService : IFolderWatcherService
    {
        private readonly IFileSystemWatcherFactory _fileSystemWatcherFactory;

        public FolderWatcherService(IFileSystemWatcherFactory fileSystemWatcherFactory)
        {
            _fileSystemWatcherFactory = fileSystemWatcherFactory;
        }

        public IObservable<FileSystemEventArgs> StartWatch(FolderRule[] folderRules)
        {
            var observable = folderRules
                .ToObservable()
                .Select(rule =>
                {
                    return Observable.Using(() => _fileSystemWatcherFactory.FromPath(rule.Path), fileSystemWatcher =>
                    {
                        return Observable.Create<FileSystemEventArgs>(observer =>
                        {
                            var d1 = Observable.Merge(
                                    Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                                        action => fileSystemWatcher.Created += action,
                                        action => fileSystemWatcher.Created -= action),
                                    Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                                        action => fileSystemWatcher.Deleted += action,
                                        action => fileSystemWatcher.Deleted -= action),
                                    Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                                        action => fileSystemWatcher.Changed += action,
                                        action => fileSystemWatcher.Changed -= action))
                                .Subscribe(observer.OnNext);

                            var d2 = Observable.FromEvent<RenamedEventHandler, RenamedEventArgs>(
                                    action => fileSystemWatcher.Renamed += action,
                                    action => fileSystemWatcher.Renamed -= action)
                                .Subscribe(observer.OnNext);

                            var d3 = Observable.FromEvent<ErrorEventHandler, ErrorEventArgs>(
                                    action => fileSystemWatcher.Error += action,
                                    action => fileSystemWatcher.Error -= action)
                                .Subscribe(args => { ; });

                            fileSystemWatcher.EnableRaisingEvents = true;
         
                            return new CompositeDisposable(d1, d2, d3);
                        });
                    });
                })
                .ToArray()
                .Select(observables => Observable.Merge(observables));
        }

        public void StopWatch()
        {
        }
    }
}