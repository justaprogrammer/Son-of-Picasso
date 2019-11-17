using System;
using System.Collections.Generic;
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
using SonOfPicasso.Data.Model;

namespace SonOfPicasso.Core.Services
{
    public class ImageContainerWatcherService : IImageContainerWatcherService, IDisposable
    {
        private const int DebounceDirectoryChangeSeconds = 2;
        private readonly Subject<IObservable<FileSystemEventArgs>> _currentStreamObservableSubject;
        private readonly CompositeDisposable _disposables;
        private readonly Subject<string> _fileDiscoveredSubject;
        private readonly IFileSystem _fileSystem;
        private readonly Subject<FileSystemEventArgs> _fileSystemEventArgsSubject;
        private readonly IFolderRulesManagementService _folderRulesManagementService;
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;

        private IObservableCache<ImageRef, string> _imageRefCache;

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
                .GroupByUntil(
                    args => args,
                    args => Observable.Timer(TimeSpan.FromSeconds(DebounceDirectoryChangeSeconds),
                        _schedulerProvider.TaskPool))
                .SelectMany(groupedObservable =>
                    groupedObservable.Select((path, count) => (path, count + 1))
                        .LastAsync())
                .Subscribe(tuple =>
                {
                    var (path, count) = tuple;
                    _logger.Verbose("Grouped Scanner Event {Path} {GroupCount}", path, count);
                    imageLocationService.GetImages(path)
                        .Subscribe(imagePath =>
                        {
                            var imageRef = _imageRefCache.Lookup(imagePath);
                            if (!imageRef.HasValue)
                            {
                                _logger.Verbose("Located Image {Path} {Exists}", imagePath, imageRef.HasValue);
                                _fileDiscoveredSubject.OnNext(imagePath);
                            }
                        });
                })
                .DisposeWith(_disposables);

            _fileSystemEventArgsSubject
                .Where(args => args.ChangeType == WatcherChangeTypes.Deleted)
                .Select(args => args.FullPath)
                .Subscribe(path => { _logger.Verbose("Delete Event {Path}", path); })
                .DisposeWith(_disposables);

            _fileSystemEventArgsSubject
                .Where(args => args.ChangeType == WatcherChangeTypes.Renamed)
                .Cast<RenamedEventArgs>()
                .Select<RenamedEventArgs, (string oldFullPath, string fullPath)>(args =>
                    (args.OldFullPath, args.FullPath))
                .Subscribe(tuple =>
                {
                    var (oldFullPath, fullPath) = tuple;
                    _logger.Verbose("Renamed Event {OldPath} {Path}", oldFullPath, fullPath);
                })
                .DisposeWith(_disposables);
        }

        public IObservable<string> FileDiscovered => _fileDiscoveredSubject.AsObservable();

        public void Dispose()
        {
            _currentStreamObservableSubject?.Dispose();
            _disposables?.Dispose();
            _fileDiscoveredSubject?.Dispose();
            _fileSystemEventArgsSubject?.Dispose();
        }

        public IObservable<Unit> Start(IObservableCache<ImageRef, string> imageRefCache)
        {
            if (imageRefCache == null) throw new ArgumentNullException(nameof(imageRefCache));

            return Observable.Defer(() =>
            {
                _logger.Verbose("Start");
                _imageRefCache = imageRefCache;

                return _folderRulesManagementService.GetFolderManagementRules();
            }).SelectMany(SubscribeWatchers);
        }

        private IObservable<Unit> SubscribeWatchers(IList<FolderRule> list)
        {
            _logger.Verbose("SubscribeWatchers");

            var subject = new Subject<string>();
            subject.DisposeWith(_disposables);

            var asObservable = subject.AsObservable();
            asObservable.Subscribe(path => { _logger.Verbose("Events Subscribed {Path}", path); })
                .DisposeWith(_disposables);

            var observables = list
                .GetTopLevelItemDictionary()
                .Select(keyValuePair => CreateWatcher(keyValuePair.Key, subject));

            var observable = observables.Merge();

            _currentStreamObservableSubject.OnNext(observable);

            return subject
                .Take(list.Count)
                .Select(s => Unit.Default)
                .LastOrDefaultAsync();
        }

        private IObservable<FileSystemEventArgs> CreateWatcher(string path, Subject<string> subject)
        {
            var pathDirectoryInfo = _fileSystem.DirectoryInfo.FromDirectoryName(path);

            return Observable.Start(() =>
                {
                    return Observable.Using(() =>
                            {
                                _logger.Verbose("Creating FileSystemWatcher {Path}", pathDirectoryInfo.Name);
                                return _fileSystem.FileSystemWatcher.FromPath(path);
                            },
                            fileSystemWatcher =>
                            {
                                _logger.Verbose("Adding event handlers to watcher {Path}", pathDirectoryInfo.Name);

                                var d1 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                        action => fileSystemWatcher.Created += action,
                                        action => fileSystemWatcher.Created -= action)
                                    .ObserveOn(_schedulerProvider.TaskPool)
                                    .Select(pattern => pattern.EventArgs);

                                var d2 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                        action => fileSystemWatcher.Deleted += action,
                                        action => fileSystemWatcher.Deleted -= action)
                                    .ObserveOn(_schedulerProvider.TaskPool)
                                    .Select(pattern => pattern.EventArgs);

                                var d3 = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                        action => fileSystemWatcher.Changed += action,
                                        action => fileSystemWatcher.Changed -= action)
                                    .ObserveOn(_schedulerProvider.TaskPool)
                                    .Select(pattern => pattern.EventArgs);

                                var d4 = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                                        action => fileSystemWatcher.Renamed += action,
                                        action => fileSystemWatcher.Renamed -= action)
                                    .ObserveOn(_schedulerProvider.TaskPool)
                                    .Select(pattern => (FileSystemEventArgs) pattern.EventArgs);

                                // https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize?view=netframework-4.8
                                // Default is 8k, Max is 64k, for best performance use multiples of 4k
                                fileSystemWatcher.InternalBufferSize = 4096 * 8;

                                fileSystemWatcher.IncludeSubdirectories = true;
                                fileSystemWatcher.EnableRaisingEvents = true;

                                subject.OnNext(path);

                                return Observable.Merge(d1, d2, d3, d4);
                            })
                        .Do(o => { },
                            () => { _logger.Verbose("Watcher Completed {Path}", path); });
                })
                .SelectMany(o => o);
        }
    }
}