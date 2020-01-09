using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        private IObservableCache<ImageRef, string> _imageRefCache;
        private CompositeDisposable _startDisposables;

        public ImageContainerWatcherService(ISchedulerProvider schedulerProvider,
            ILogger logger,
            IFolderRulesManagementService folderRulesManagementService,
            IImageLocationService imageLocationService,
            IFileSystem fileSystem)
        {
            _schedulerProvider = schedulerProvider;
            _logger = logger;
            _folderRulesManagementService = folderRulesManagementService;
            _fileSystem = fileSystem;
            _disposables = new CompositeDisposable();

            _fileSystemEventArgsSubject = new Subject<FileSystemEventArgs>();

            _currentStreamObservableSubject = new Subject<IObservable<FileSystemEventArgs>>();
            _currentStreamObservableSubject.Switch()
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

        public IObservable<Unit> Start(IObservableCache<ImageRef, string> imageRefCache)
        {
            if (imageRefCache == null) throw new ArgumentNullException(nameof(imageRefCache));

            return Observable.Defer(() =>
                {
                    _logger.Verbose("Start");

                    _startDisposables?.Dispose();
                    _startDisposables = new CompositeDisposable();

                    _imageRefCache = imageRefCache;

                    return _folderRulesManagementService.GetFolderManagementRules();
                }).Select(list =>
                {
                    var dictionary = list
                        .GetTopLevelItemDictionary();

                    _logger.Verbose("Creating Watchers");

                    var watchers = dictionary.Select(keyValuePair =>
                    {
                        var path = keyValuePair.Key;

                        var fileSystemWatcher = _fileSystem.FileSystemWatcher
                            .FromPath(path)
                            .DisposeWith(_startDisposables);

                        var d1 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                action => fileSystemWatcher.Created += action,
                                action => { })
                            .SubscribeOn(_schedulerProvider.TaskPool)
                            .Select(pattern => pattern.EventArgs);

                        var d2 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                action => fileSystemWatcher.Deleted += action,
                                action => { })
                            .SubscribeOn(_schedulerProvider.TaskPool)
                            .Select(pattern => pattern.EventArgs);

                        var d3 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                action => fileSystemWatcher.Changed += action,
                                action => { })
                            .SubscribeOn(_schedulerProvider.TaskPool)
                            .Select(pattern => pattern.EventArgs);

                        var d4 = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                                action => fileSystemWatcher.Renamed += action,
                                action => { })
                            .SubscribeOn(_schedulerProvider.TaskPool)
                            .Select(pattern => (FileSystemEventArgs) pattern.EventArgs);

                        // https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize?view=netframework-4.8
                        // Default is 8k, Max is 64k, for best performance use multiples of 4k
                        fileSystemWatcher.InternalBufferSize = 4096 * 8;

                        fileSystemWatcher.IncludeSubdirectories = true;
                        fileSystemWatcher.EnableRaisingEvents = true;

                        var observables = new[] {d1, d2, d3, d4};
                        return (fileSystemWatcher, observables);
                    }).ToArray();

                    var allObservables = watchers.SelectMany(tuple => tuple.observables).ToArray();

                    var observable = Observable.Merge(allObservables);
                    _currentStreamObservableSubject.OnNext(observable);

                    return Unit.Default;
                })
                .Do(unit => _logger.Verbose("Started"));
        }

        public void Stop()
        {
            _currentStreamObservableSubject.OnNext(Observable.Never<FileSystemEventArgs>());

            _startDisposables?.Dispose();
            _startDisposables = null;
        }
    }
}