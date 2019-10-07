using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SonOfPicasso.Core.Services
{
    public class PathWatcherService
    {
        private readonly IFileSystem _fileSystem;
        private Subject<FileSystemEventArgs> _subject;
        private Dictionary<string, (IFileSystemWatcher, CompositeDisposable)> _watchers;

        public PathWatcherService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _subject = new Subject<FileSystemEventArgs>();

            _watchers = new Dictionary<string, (IFileSystemWatcher, CompositeDisposable)>();
        }

        public IObservable<FileSystemEventArgs> Events => _subject.AsObservable();

        public void WatchPath(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var key = path.ToLowerInvariant();

            if(_watchers.ContainsKey(key))
                throw new InvalidOperationException();

            var fileSystemWatcher = _fileSystem.FileSystemWatcher.FromPath(path);

            var created = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    a => fileSystemWatcher.Created += a, 
                    action => fileSystemWatcher.Created -= action)
                .Subscribe(pattern => _subject.OnNext(pattern.EventArgs));

            var changed = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    a => fileSystemWatcher.Changed += a, 
                    action => fileSystemWatcher.Changed -= action)
                .Subscribe(pattern => _subject.OnNext(pattern.EventArgs));

            var deleted = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    a => fileSystemWatcher.Deleted += a, 
                    action => fileSystemWatcher.Deleted -= action)
                .Subscribe(pattern => _subject.OnNext(pattern.EventArgs));

            var renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                    a => fileSystemWatcher.Renamed += a, 
                    action => fileSystemWatcher.Renamed -= action)
                .Subscribe(pattern => _subject.OnNext(pattern.EventArgs));

            var compositeDisposable = new CompositeDisposable(created, changed, deleted, renamed);

            _watchers.Add(key, (fileSystemWatcher, compositeDisposable));
        }

        public void ClearPath(string path)
        {
            var key = path.ToLowerInvariant();
            var (fileSystemWatcher, compositeDisposable) = _watchers[key];
            _watchers.Remove(key);
            fileSystemWatcher.EnableRaisingEvents = false;
            compositeDisposable.Dispose();
            fileSystemWatcher.Dispose();
        }
    }
}