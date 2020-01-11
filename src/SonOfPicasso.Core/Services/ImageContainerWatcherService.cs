using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Documents;
using DynamicData;
using Serilog;
using SonOfPicasso.Core.Extensions;
using SonOfPicasso.Core.Interfaces;
using SonOfPicasso.Core.Model;
using SonOfPicasso.Core.Scheduling;

namespace SonOfPicasso.Core.Services
{
    public sealed class ImageContainerWatcherService : IImageContainerWatcherService, IDisposable
    {
        private const int DebounceDirectoryChangeSeconds = 2;
        private readonly Subject<IObservable<FileSystemEventArgs>> _currentStreamObservableSubject;
        private readonly CompositeDisposable _disposables;
        private readonly Subject<string> _fileDeletedSubject;
        private readonly Subject<string> _fileDiscoveredSubject;
        private readonly Subject<(string oldFullPath, string fullPath)> _fileRenamedSubject;
        private readonly IFileSystem _fileSystem;
        private readonly Subject<FileSystemEventArgs> _fileSystemEventArgsSubject;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        private IObservableCache<ImageRef, string> _imageRefCache;
        private CompositeDisposable _startDisposables;

        public ImageContainerWatcherService(ISchedulerProvider schedulerProvider,
            ILogger logger,
            IImageLocationService imageLocationService,
            IFileSystem fileSystem)
        {
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            _fileSystem = fileSystem;
            _disposables = new CompositeDisposable();

            _fileSystemEventArgsSubject = new Subject<FileSystemEventArgs>();

            _currentStreamObservableSubject = new Subject<IObservable<FileSystemEventArgs>>();
            _currentStreamObservableSubject
                .Switch()
                .Subscribe(args => { _fileSystemEventArgsSubject.OnNext(args); });

            _fileDiscoveredSubject = new Subject<string>();
            _fileDeletedSubject = new Subject<string>();
            _fileRenamedSubject = new Subject<(string oldFullPath, string fullPath)>();

            _fileSystemEventArgsSubject.Subscribe(args =>
                {
                    var containerPath = args.FullPath.Substring(0, args.FullPath.Length - args.Name.Length);
                    var containerName = _fileSystem.DirectoryInfo.FromDirectoryName(containerPath).Name;

                    if (args.ChangeType != WatcherChangeTypes.Deleted)
                        if (_fileSystem.Directory.Exists(args.FullPath))
                        {
                            _logger.Verbose("Directory Event {Name} {FullPath} {ChangeType}", containerName, args.Name,
                                args.ChangeType);
                            return;
                        }

                    _logger.Verbose("File Event {Name} {FullPath} {ChangeType}", containerName, args.Name,
                        args.ChangeType);
                })
                .DisposeWith(_disposables);

            _fileSystemEventArgsSubject
                .Where(args => args.ChangeType == WatcherChangeTypes.Created ||
                               args.ChangeType == WatcherChangeTypes.Changed)
                .Select(args =>
                {
                    if (_fileSystem.Directory.Exists(args.FullPath)) return args.FullPath;

                    return _fileSystem.FileInfo.FromFileName(args.FullPath).DirectoryName;
                })
                .Buffer(TimeSpan.FromSeconds(DebounceDirectoryChangeSeconds))
                .SelectMany(list => list.GroupBy(s => s).Select(grouping => (grouping.Key, grouping.Count() + 1)))
                .Subscribe(tuple =>
                {
                    var (path, count) = tuple;
                    _logger.Verbose("Grouped Scanner Event {Path} {GroupCount}", path, count);
                    imageLocationService.GetImages(path)
                        .Subscribe(fileInfo =>
                        {
                            var imageRef = _imageRefCache.Lookup(fileInfo.FullName);
                            if (!imageRef.HasValue)
                            {
                                _logger.Verbose("Discovered {Path}", fileInfo.FullName);
                                _fileDiscoveredSubject.OnNext(fileInfo.FullName);
                            }
                            else if (imageRef.Value.LastWriteTime != fileInfo.LastWriteTimeUtc ||
                                     imageRef.Value.CreationTime != fileInfo.CreationTimeUtc)
                            {
                                _logger.Verbose("File Updated {Path}", fileInfo.FullName);
                                _fileDiscoveredSubject.OnNext(fileInfo.FullName);
                            }
                        });
                })
                .DisposeWith(_disposables);

            _fileSystemEventArgsSubject
                .Where(args => args.ChangeType == WatcherChangeTypes.Deleted)
                .Select(args => args.FullPath)
                .Subscribe(path =>
                {
                    var imageRef = _imageRefCache.Lookup(path);
                    if (imageRef.HasValue)
                    {
                        _logger.Verbose("Deleted {Path}", path);
                        _fileDeletedSubject.OnNext(path);
                    }
                })
                .DisposeWith(_disposables);

            _fileSystemEventArgsSubject
                .Where(args => args.ChangeType == WatcherChangeTypes.Renamed)
                .Cast<RenamedEventArgs>()
                .Select<RenamedEventArgs, (string oldFullPath, string fullPath)>(args =>
                    (args.OldFullPath, args.FullPath))
                .Subscribe(tuple =>
                {
                    var (oldFullPath, fullPath) = tuple;
                    var imageRef = _imageRefCache.Lookup(oldFullPath);
                    if (_fileSystem.File.Exists(fullPath))
                    {
                        if (imageRef.HasValue)
                        {
                            _logger.Verbose("Renamed {OldPath} {Path}", oldFullPath, fullPath);
                            _fileRenamedSubject.OnNext((oldFullPath, fullPath));
                        }
                        else
                        {
                            _logger.Verbose("Renamed; Previously unknown; Considering Discovered {Path}", fullPath);
                            _fileDiscoveredSubject.OnNext(fullPath);
                        }
                    }
                })
                .DisposeWith(_disposables);
        }

        public void Dispose()
        {
            _currentStreamObservableSubject?.Dispose();
            _disposables?.Dispose();
            _fileDeletedSubject?.Dispose();
            _fileDiscoveredSubject?.Dispose();
            _fileRenamedSubject?.Dispose();
            _fileSystemEventArgsSubject?.Dispose();
            _imageRefCache?.Dispose();
            _startDisposables?.Dispose();
        }

        public IObservable<string> FileDiscovered => _fileDiscoveredSubject.AsObservable();

        public IObservable<string> FileDeleted => _fileDeletedSubject.AsObservable();

        public IObservable<(string oldFullPath, string fullPath)> FileRenamed => _fileRenamedSubject.AsObservable();

        public void Start(IObservableCache<ImageRef, string> imageRefCache, IList<string> paths)
        {
            _logger.Verbose("Start");

            if (imageRefCache == null) throw new ArgumentNullException(nameof(imageRefCache));

            _startDisposables?.Dispose();
            _startDisposables = new CompositeDisposable();

            _imageRefCache = imageRefCache;

            _logger.Verbose("Creating {Count} Watchers", paths.Count);

            var watchers = new IFileSystemWatcher[paths.Count];
            var observables = new IObservable<FileSystemEventArgs>[paths.Count * 4];

            for (var index = 0; index < paths.Count; index++)
            {
                var path = paths[index];

                _logger.Verbose("Creating Watcher {Path}", path);

                var watcher = _fileSystem.FileSystemWatcher
                    .FromPath(path)
                    .DisposeWith(_startDisposables);

                watcher.IncludeSubdirectories = true;

                // https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize?view=netframework-4.8
                // Default is 8k, Max is 64k, for best performance use multiples of 4k
                watcher.InternalBufferSize = 4096 * 1;

                watchers[index] = watcher;

                var observableIndex = index * 4;
                observables[observableIndex++] = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        action => watcher.Created += action,
                        action => { })
                    .SubscribeOn(_schedulerProvider.TaskPool)
                    .Select(pattern => pattern.EventArgs);

                observables[observableIndex++] = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        action => watcher.Deleted += action,
                        action => { })
                    .SubscribeOn(_schedulerProvider.TaskPool)
                    .Select(pattern => pattern.EventArgs);

                observables[observableIndex++] = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        action => watcher.Changed += action,
                        action => { })
                    .SubscribeOn(_schedulerProvider.TaskPool)
                    .Select(pattern => pattern.EventArgs);

                observables[observableIndex] = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                        action => watcher.Renamed += action,
                        action => { })
                    .SubscribeOn(_schedulerProvider.TaskPool)
                    .Select(pattern => (FileSystemEventArgs) pattern.EventArgs);
            }

            _currentStreamObservableSubject.OnNext(Observable.Merge(observables));

            foreach (var watcher in watchers)
            {
                watcher.EnableRaisingEvents = true;
            }

            _logger.Verbose("Started");
        }

        public void Stop()
        {
            _currentStreamObservableSubject.OnNext(Observable.Never<FileSystemEventArgs>());

            _startDisposables?.Dispose();
            _startDisposables = null;
        }
    }
}